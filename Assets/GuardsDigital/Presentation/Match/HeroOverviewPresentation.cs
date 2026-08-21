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
    /// One printed value as the tray draws it: the compact token its channel is shown as, the amount,
    /// and the words for both.
    ///
    /// Milestone 8.5a. It is a formatting of one <see cref="PrintedValueDefinition"/> and nothing more -
    /// never a modified total, never a sum across actions - because the tray draws the card, and what a
    /// hero's items raise is the hero's business.
    /// </summary>
    public sealed class PrintedValueChipViewModel
    {
        public PrintedValueChipViewModel(PrintedValueChannel channel, int amount)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A printed value names its channel.", nameof(channel));
            }

            Channel = channel;
            Amount = amount;
            Token = MatchHudTheme.ChannelToken(channel);
            AmountLabel = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Label = $"{Token} {AmountLabel}";
            AssistiveLabel = $"{MatchHudTheme.ChannelName(channel)} {AmountLabel}";
        }

        public PrintedValueChannel Channel { get; }
        public int Amount { get; }

        /// <summary>The three-letter token, which is what the card shows beside the number.</summary>
        public string Token { get; }

        public string AmountLabel { get; }

        /// <summary>`RAD 3` - the token and the amount as one readable pair.</summary>
        public string Label { get; }

        /// <summary>`Radius 3` - the same pair in words, for the hover and assistive reading.</summary>
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// The printed face of one card, read into the four homes the milestone 8.5a mockup gives it:
    /// Initiative on its own flag, the primary action's own numeric value on the primary band, the p.9
    /// Range or Radius icon on the spatial badge, and every value belonging to an alternate action in
    /// the secondary strip - with the words the face prints below them.
    ///
    /// <para><b>Every grouping is typed.</b> Which region a value belongs to is decided by the action it
    /// is printed on and by its <see cref="PrintedValueChannel"/>, never by the card's name and never by
    /// reading the printed sentence. That is the same rule the rest of this project keeps at the card
    /// seam: this build has no printed-text parser, and a presenter that grew one would be the first.</para>
    ///
    /// <para><b>It says nothing the definition does not.</b> A face that prints no rules text, and a
    /// synthetic fixture with no source face at all, are two different absences and are drawn as two
    /// different explicit statements rather than as invented copy or as an empty rectangle.</para>
    /// </summary>
    public sealed class TrayCardFaceViewModel
    {
        public TrayCardFaceViewModel(
            PrintedValueChipViewModel initiative,
            string primaryActionLabel,
            PrintedValueChipViewModel primaryValue,
            PrintedValueChipViewModel spatialValue,
            IEnumerable<PrintedValueChipViewModel> secondaryValues,
            CardSourceContentKind printedTextKind,
            string printedText)
        {
            Initiative = initiative;
            PrimaryActionLabel = string.IsNullOrWhiteSpace(primaryActionLabel)
                ? throw new ArgumentException(
                    "A tray card states what its primary action is, or that it has none.",
                    nameof(primaryActionLabel))
                : primaryActionLabel;
            PrimaryValue = primaryValue;
            SpatialValue = spatialValue;
            SecondaryValues = Array.AsReadOnly((secondaryValues ??
                Enumerable.Empty<PrintedValueChipViewModel>()).ToArray());
            PrintedTextKind = printedTextKind;
            PrintedText = printedText ?? string.Empty;

            // The two absences, each stated as itself. "This face prints none" is a fact about a card
            // that was read; "no source face" is a fact about a fixture that was invented. Collapsing
            // them would let a synthetic card in a Play Mode scene read as a verified card that happens
            // to be silent.
            EmptyTextLabel = printedTextKind == CardSourceContentKind.PrintsNoText
                ? "THIS FACE PRINTS NO RULES TEXT"
                : printedTextKind == CardSourceContentKind.Transcribed
                    ? string.Empty
                    : "NO SOURCE FACE";

            var parts = new List<string>();
            if (Initiative != null)
            {
                parts.Add(Initiative.Label);
            }

            parts.Add(PrimaryActionLabel);
            if (PrimaryValue != null)
            {
                parts.Add(PrimaryValue.Label);
            }

            if (SpatialValue != null)
            {
                parts.Add(SpatialValue.Label);
            }

            foreach (var secondary in SecondaryValues)
            {
                parts.Add(secondary.Label);
            }

            ValueLine = string.Join(" · ", parts);
        }

        /// <summary>The card's printed Initiative, or null for p.16's Ultimate, which prints none.</summary>
        public PrintedValueChipViewModel Initiative { get; }

        /// <summary>
        /// What the card's primary action is, in typed terms: its kind, p.15's Basic supertype where the
        /// definition states one, and the one qualifier the type carries - p.13's Ranged classification
        /// on an Attack, p.14's Block on a Defense.
        /// </summary>
        public string PrimaryActionLabel { get; }

        /// <summary>The primary action's own combat value, or null when it prints none.</summary>
        public PrintedValueChipViewModel PrimaryValue { get; }

        /// <summary>p.9's one Range or Radius icon, or null on a face that prints neither.</summary>
        public PrintedValueChipViewModel SpatialValue { get; }

        /// <summary>Every printed value belonging to an action that is not the primary one.</summary>
        public IReadOnlyList<PrintedValueChipViewModel> SecondaryValues { get; }

        public CardSourceContentKind PrintedTextKind { get; }

        /// <summary>The transcribed words, verbatim, and empty for either absence.</summary>
        public string PrintedText { get; }

        /// <summary>What the rules panel says instead of words, and empty when there are words.</summary>
        public string EmptyTextLabel { get; }

        /// <summary>
        /// The whole face as one line, in the same order the regions are drawn in. It is the hover and
        /// assistive reading of a card whose values are spatially separated on screen.
        /// </summary>
        public string ValueLine { get; }
    }

    /// <summary>
    /// One card of the viewed hero's current round, as the tray draws it.
    ///
    /// It carries the card's authoritative colour rather than a swatch chosen here, and the colour's own
    /// name beside it, so the button is never colour alone. <see cref="PlayedTurnIndex"/> is the turn the
    /// round's assignment history recorded, and is zero for every group but <see cref="TrayCardGroup.Played"/>.
    /// <see cref="Face"/> is milestone 8.5a's: the printed face itself, grouped into the regions the card
    /// draws it in.
    /// </summary>
    public sealed class TrayCardViewModel
    {
        public TrayCardViewModel(
            CardInstanceId cardInstanceId,
            string displayName,
            CardColor color,
            TrayCardFaceViewModel face,
            TrayCardGroup group,
            int playedTurnIndex,
            bool isSelectable,
            bool isSelected,
            bool isActiveEffectSource = false,
            bool isResolving = false)
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
            Face = face ?? throw new ArgumentNullException(nameof(face));
            Group = group;
            PlayedTurnIndex = playedTurnIndex;
            IsSelectable = isSelectable;
            IsSelected = isSelected;
            IsActiveEffectSource = isActiveEffectSource;
            IsResolving = isResolving;
            ColorName = MatchHudTheme.ColorName(color);
            HistoryLabel = group == TrayCardGroup.Played
                ? $"PLAYED · TURN {playedTurnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : group == TrayCardGroup.Discarded
                    ? "DISCARDED"
                    : "AVAILABLE";
            ActiveLabel = isActiveEffectSource ? "ACTIVE" : string.Empty;
            AssistiveLabel = $"{DisplayName} - {ColorName} - {Face.ValueLine} - {HistoryLabel}" +
                (isActiveEffectSource ? " - ACTIVE EFFECT IN EFFECT" : string.Empty);
        }

        public CardInstanceId CardInstanceId { get; }
        public string DisplayName { get; }
        public CardColor Color { get; }
        public string ColorName { get; }

        /// <summary>Milestone 8.5a: the printed face, in the regions the card draws it in.</summary>
        public TrayCardFaceViewModel Face { get; }

        /// <summary>
        /// The card's printed Initiative and action values as one line. It is the assistive and hover
        /// reading of <see cref="Face"/>, which is the same content spread across the card's regions -
        /// one grouping, read twice, so the two can never disagree.
        /// </summary>
        public string ValueLine => Face.ValueLine;

        /// <summary>The whole card as one line: name, colour, printed values, and lifecycle.</summary>
        public string AssistiveLabel { get; }

        public TrayCardGroup Group { get; }
        public int PlayedTurnIndex { get; }

        /// <summary>
        /// Whether the authority currently offers this card for commitment. A card that is not
        /// selectable still draws, because reading what a hero has played and discarded is the point of
        /// the tray; it simply carries no command.
        /// </summary>
        public bool IsSelectable { get; }

        public bool IsSelected { get; }

        /// <summary>
        /// Milestone 8.5b: whether the authority reports at least one live Active effect sourced by
        /// this card instance.
        ///
        /// It is a projected fact, not a guess: a card whose definition could create an effect, or a
        /// card that made one which has since ended, is not this. It is orthogonal to every other state
        /// a card carries - a card can be selected, unavailable, played and active at once - which is
        /// why it has its own treatment and its own word rather than a shade of an existing one.
        /// </summary>
        public bool IsActiveEffectSource { get; }

        /// <summary>`ACTIVE`, or empty when nothing this card sourced is live.</summary>
        public string ActiveLabel { get; }

        /// <summary>
        /// Whether this is the card the current turn is being resolved from.
        ///
        /// A resolving card carries no commitment command - the round is past choosing - but dimming it
        /// as merely unavailable was wrong: it is the card in play, and the user's 2026-08-21 report is
        /// that it should read at full strength while its turn runs. It is separate from
        /// <see cref="IsSelectable"/> because the two answer different questions: what may be committed
        /// now, and what is being performed now.
        /// </summary>
        public bool IsResolving { get; }

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
        public HeroTurnSlotViewModel(
            int turnIndex,
            CardColor color,
            bool isOccupied,
            TrayCardViewModel playedCard = null)
        {
            if (turnIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            if (isOccupied != (playedCard != null))
            {
                throw new ArgumentException(
                    "An occupied slot names the card played on its turn, and only an occupied one does.",
                    nameof(playedCard));
            }

            TurnIndex = turnIndex;
            Color = color;
            IsOccupied = isOccupied;
            PlayedCard = playedCard;
            AssistiveLabel = isOccupied
                ? $"Turn {turnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}: " +
                    $"{MatchHudTheme.ColorName(color)} card - {playedCard.DisplayName}"
                : $"Turn {turnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}: empty";
        }

        public int TurnIndex { get; }
        public CardColor Color { get; }
        public bool IsOccupied { get; }

        /// <summary>
        /// The card this hero played on that turn, as the tray draws it, or null for an empty slot.
        ///
        /// Milestone 8.5a, by the user's 2026-08-21 decision: resting the pointer on an occupied slot
        /// reveals that card's whole printed face. The slot is the only place a round's earlier turns
        /// stay visible once the tray has moved on, and until this it carried a swatch and a word.
        /// </summary>
        public TrayCardViewModel PlayedCard { get; }

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
                    : new HeroTurnSlotViewModel(
                        turnIndex,
                        ColorOf(projection, card),
                        true,
                        TrayCard(projection, card, TrayCardGroup.Played, turnIndex, false, false)));
            }

            return Array.AsReadOnly(slots.ToArray());
        }

        /// <summary>
        /// One card instance as a tray card, built the one way both surfaces that draw a card build one.
        ///
        /// Milestone 8.5a added the second surface - a hero rail's turn slot reveals the card played on
        /// its turn - so this joined <see cref="ColorOf"/> and <see cref="PlayedTurnIndex"/> as a reading
        /// this class states once. Two constructions of the same card that drifted apart would show a
        /// player two different faces for one card and neither would be obviously wrong.
        /// </summary>
        public static TrayCardViewModel TrayCard(
            MatchSessionProjection projection,
            CardInstance card,
            TrayCardGroup group,
            int playedTurnIndex,
            bool isSelectable,
            bool isSelected,
            bool isResolving = false)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (card == null) throw new ArgumentNullException(nameof(card));
            var definition = projection.Definitions
                .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            return new TrayCardViewModel(
                card.Id,
                definition?.DisplayName ?? card.Id.Value,
                definition?.Progression.Color ?? CardColor.None,
                ReadFace(definition),
                group,
                playedTurnIndex,
                isSelectable,
                isSelected,
                // Milestone 8.5b: asked of the projection, never derived here. Presentation does not
                // know what an Active effect is, only which cards the authority says are sourcing one.
                projection.IsActiveEffectSource(card.Id),
                isResolving);
        }

        /// <summary>
        /// Milestone 8.5a: one card definition read into the four value homes and the printed words its
        /// tray card draws, from typed content alone.
        ///
        /// <para>It is definition content formatted, never a modified total: a hero's item bonuses raise
        /// a card's totals, and this face is the card. A definition the projection does not carry - which
        /// only happens for an instance whose content was never registered - reads as a face that states
        /// nothing, rather than faulting a whole tray over one card.</para>
        /// </summary>
        public static TrayCardFaceViewModel ReadFace(CardDefinition definition)
        {
            if (definition == null)
            {
                return new TrayCardFaceViewModel(
                    null,
                    "NO CARD DEFINITION",
                    null,
                    null,
                    Array.Empty<PrintedValueChipViewModel>(),
                    CardSourceContentKind.NotSourced,
                    string.Empty);
            }

            var primary = PrimaryAction(definition);
            var spatial = SpatialValue(definition, primary);
            return new TrayCardFaceViewModel(
                definition.Initiative.IsDefined
                    ? new PrintedValueChipViewModel(
                        PrintedValueChannel.Initiative, definition.Initiative.Value)
                    : null,
                PrimaryActionLabel(definition, primary),
                PrimaryValue(primary),
                spatial,
                SecondaryValues(definition),
                definition.SourceContent?.Kind ?? CardSourceContentKind.NotSourced,
                definition.SourceContent?.PrintedText ?? string.Empty);
        }

        /// <summary>
        /// The action rulebook p.9 prints as the card's primary. It is read from
        /// <see cref="ActionDefinitionOrigin"/>, which is the only place a definition says so.
        /// </summary>
        private static ActionDefinition PrimaryAction(CardDefinition definition) =>
            definition.ActionDefinitions.FirstOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        /// <summary>
        /// The primary action's own numeric value - its Attack or Defense number - which is the value
        /// the action's own algorithm consumes. p.9's Range/Radius icon sits beside a primary action
        /// without being one of its numbers, so it is excluded here and drawn on the spatial badge.
        /// </summary>
        private static PrintedValueChipViewModel PrimaryValue(ActionDefinition primary) =>
            primary == null
                ? null
                : Chip(primary.BaseValues, channel =>
                    channel != PrintedValueChannel.Range && channel != PrintedValueChannel.Radius);

        /// <summary>
        /// p.9's one Range or Radius icon: "Some primary actions have a Range or Radius icon and a value
        /// on the right side of the card". It rides the primary action on every card that has one, and
        /// p.16's Ultimate - which is not an action at all - carries its own on its permanent rule.
        /// </summary>
        private static PrintedValueChipViewModel SpatialValue(
            CardDefinition definition, ActionDefinition primary)
        {
            var printed = primary == null
                ? (IEnumerable<PrintedValueDefinition>)definition.UltimateRule?.PrintedValues
                : primary.BaseValues;
            return Chip(
                printed,
                channel => channel == PrintedValueChannel.Range ||
                    channel == PrintedValueChannel.Radius);
        }

        /// <summary>
        /// The first printed value whose channel the caller is asking about, as a chip, or null when the
        /// face prints none. <see cref="PrintedValueDefinition"/> is a value type, so an absence is asked
        /// for as one rather than read out of a default instance whose channel would be
        /// <see cref="PrintedValueChannel.None"/>.
        /// </summary>
        private static PrintedValueChipViewModel Chip(
            IEnumerable<PrintedValueDefinition> printedValues,
            Func<PrintedValueChannel, bool> wanted)
        {
            if (printedValues == null)
            {
                return null;
            }

            foreach (var value in printedValues)
            {
                if (value.Channel != PrintedValueChannel.None && wanted(value.Channel))
                {
                    return new PrintedValueChipViewModel(value.Channel, value.Amount);
                }
            }

            return null;
        }

        /// <summary>
        /// Every printed value belonging to an action that is not the primary one - p.9's secondary
        /// Movement and p.14's secondary Defense - in the one channel order the whole HUD reads values
        /// in.
        ///
        /// Only actions the card itself prints qualify. A general-rule secondary and a text-granted
        /// action are offered to a hero holding the card rather than printed on its face, so putting
        /// their values in a printed-value region would be showing a number the card does not have.
        /// </summary>
        private static IEnumerable<PrintedValueChipViewModel> SecondaryValues(CardDefinition definition) =>
            definition.ActionDefinitions
                .Where(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary ||
                    action.Origin == ActionDefinitionOrigin.Reaction)
                .SelectMany(action => action.BaseValues)
                .OrderBy(value => MatchHudTheme.ChannelOrder.ToList().IndexOf(value.Channel))
                .Select(value => new PrintedValueChipViewModel(value.Channel, value.Amount))
                .ToArray();

        /// <summary>
        /// What the card's primary action is, in words a player reads off the printed face: p.15's Basic
        /// supertype where the definition states one, the action's kind, and the one qualifier its own
        /// type carries.
        ///
        /// The qualifiers are typed facts and only typed facts. p.13's Ranged classification lives on
        /// <see cref="ActionDefinition.AttackRange"/> and p.14's Block on
        /// <see cref="ActionDefinition.DefenseResolution"/>; anything else a face happens to say about
        /// itself is in its printed words, where the player can read it, and is never inferred here.
        /// </summary>
        private static string PrimaryActionLabel(CardDefinition definition, ActionDefinition primary)
        {
            if (primary == null)
            {
                // p.16's Ultimate is the one card with no action at all: it is placed beside the
                // dashboard and its ability is always in effect, so naming its supertype is naming the
                // whole of what it is.
                return definition.Progression.IsUltimate ? "ULTIMATE" : "NO PRIMARY ACTION";
            }

            var kind = primary.Kind.ToString().ToUpperInvariant();
            if (primary.Kind == ActionKind.Attack && primary.AttackRange == AttackRangeKind.Ranged)
            {
                kind += " · RANGED";
            }
            else if (primary.Kind == ActionKind.Defense &&
                primary.DefenseResolution == PrintedDefenseResolution.Block)
            {
                kind += " · BLOCK";
            }

            return definition.Progression.IsBasic ? "BASIC " + kind : kind;
        }
    }
}
