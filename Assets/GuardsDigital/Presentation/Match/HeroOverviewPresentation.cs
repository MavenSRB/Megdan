using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Units.Heroes;

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
        Discarded = 3,

        /// <summary>
        /// Still in the hero's deck, which p.5 leaves face-down and p.15's Level Up and p.16's unlock are
        /// the only ways out of.
        ///
        /// Milestone 8.8a. The tray never produces it - the tray draws one hero's current round, and a
        /// deck card is not in one - but p.15's upgrade prompt draws the candidate it is offering as a
        /// whole card face, and the card it is offering is in the deck. Saying so is the alternative to
        /// borrowing <see cref="Available"/>, which would put the word `AVAILABLE` on the lifecycle band
        /// of a card that is not in anybody's hand.
        /// </summary>
        Deck = 4,

        /// <summary>
        /// Faceup in rulebook p.16's Ultimate area beside the dashboard: unlocked, permanent, and not
        /// an action anybody plays.
        ///
        /// Milestone 8.15. Only the hero rail's Ultimate marker produces it, for milestone 8.8a's
        /// reason: every other word the lifecycle band can carry claims something - a turn, a pile, a
        /// hand, a deck - that p.16's card is exactly not in.
        /// </summary>
        Unlocked = 5
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
            CardDefinitionReference definitionReference,
            string displayName,
            CardColor color,
            TrayCardFaceViewModel face,
            TrayCardGroup group,
            int playedTurnIndex,
            bool isSelectable,
            bool isSelected,
            bool isActiveEffectSource = false,
            bool isResolving = false,
            bool isAwaitedAnswer = false,
            bool isDimmedByResolution = false,
            bool isFacedown = false,
            IReadOnlyList<SpellCardViewModel> castableSpells = null)
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

            if (isAwaitedAnswer && isDimmedByResolution)
            {
                throw new ArgumentException(
                    "A card that shines as the awaited answer is never also the dimmed rest of the " +
                    "hand - the SEVENTH amendment's resolution focus draws the eye to one and away " +
                    "from the other, so the two treatments are exclusive by construction.",
                    nameof(isDimmedByResolution));
            }

            CardInstanceId = cardInstanceId;
            DefinitionReference = definitionReference;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Color = color;
            Face = face ?? throw new ArgumentNullException(nameof(face));
            Group = group;
            PlayedTurnIndex = playedTurnIndex;
            IsSelectable = isSelectable;
            IsSelected = isSelected;
            IsActiveEffectSource = isActiveEffectSource;
            IsResolving = isResolving;
            IsAwaitedAnswer = isAwaitedAnswer;
            IsDimmedByResolution = isDimmedByResolution;
            IsFacedown = isFacedown;
            CastableSpells = castableSpells ?? Array.Empty<SpellCardViewModel>();
            ColorName = MatchHudTheme.ColorName(color);
            // Milestone 8.7a. A card the round is still resolving is not history yet, and the round's
            // assignment history names its turn from the moment that turn starts - so a card in play
            // read `PLAYED · TURN 1` while it was being played. The lifecycle line says what the card is
            // doing instead, on every surface that draws this one view model: the tray, the rail's
            // current-card marker, and the face that marker reveals.
            HistoryLabel = isResolving
                ? "RESOLVING"
                : group == TrayCardGroup.Played
                    ? $"PLAYED · TURN {playedTurnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                    : group == TrayCardGroup.Discarded
                        ? "DISCARDED"
                        : group == TrayCardGroup.Deck
                            ? "IN DECK"
                            : group == TrayCardGroup.Unlocked
                                ? "UNLOCKED"
                                : "AVAILABLE";
            ActiveLabel = isActiveEffectSource ? "ACTIVE" : string.Empty;
            AssistiveLabel = $"{DisplayName} - {ColorName} - {Face.ValueLine} - {HistoryLabel}" +
                (isActiveEffectSource ? " - ACTIVE EFFECT IN EFFECT" : string.Empty) +
                // Milestone 8.75e. NOTHING follows about which of a two-text card's printed lines is
                // live. Milestone 8.75b appended one; the user dropped that reading along with the
                // corner badge it explained ("Drop the badge and rely on the coin strip alone"), so
                // EVERY card of every hero reads exactly as it did before her.
                (isAwaitedAnswer ? " - AWAITING YOUR ANSWER" : string.Empty) +
                // Milestone 9.32g. Spoken and in the tooltip only: the drawn mini says it by its hatch,
                // with no caption (the user's 2026-10-02 amendment to mockup B).
                (isFacedown ? " - FACEDOWN" : string.Empty);
        }

        public CardInstanceId CardInstanceId { get; }

        /// <summary>
        /// The immutable projected identity that selects optional presentation artwork. It does not
        /// replace any typed face content and may be undefined only for the existing missing-definition
        /// fallback shape.
        /// </summary>
        public CardDefinitionReference DefinitionReference { get; }
        public string DisplayName { get; }
        public CardColor Color { get; }
        public string ColorName { get; }

        /// <summary>Milestone 8.5a: the printed face, in the regions the card draws it in.</summary>
        public TrayCardFaceViewModel Face { get; }

        /// <summary>
        /// Spell faces declared by this card's typed cast arms, in printed order. A face whose public
        /// instance is already removed from its owner's spellbook is greyed; an ordinary card carries
        /// an empty list. This is projected presentation data, not a card-name or printed-text reading.
        /// </summary>
        public IReadOnlyList<SpellCardViewModel> CastableSpells { get; }

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

        /// <summary>
        /// Milestone 8.35m (the SEVENTH amendment's card scope and resolution focus). Whether this
        /// card is currently a legal answer to input the match is waiting on FROM THE VIEWER - their
        /// own hand during their commitment, an offered Defense/effect-choice/removal-prevention
        /// candidate, a p.15 leveling upgrade option, or the viewer's own card while it resolves (the
        /// resolution-focus case: the resolving card is the focal point of what the match is waiting on
        /// them to finish, exactly as much as a clickable candidate is).
        ///
        /// <para>Every offered-card surface - <c>CreateCardFace</c>'s callers and
        /// <c>CurrentRoundCards.DeckCard</c> - sets this unconditionally true, because being drawn
        /// there at all already means the authority is offering the card; the ordinary hand tray sets
        /// it from the same commitment-offer fact <see cref="IsSelectable"/> already reads, plus
        /// whichever card is <see cref="IsResolving"/>. A history surface - a discarded card, a
        /// resolved turn slot, the rail's Ultimate marker - never sets it, so hovering one of those to
        /// read its face through the same shared builder never shines it.</para>
        /// </summary>
        public bool IsAwaitedAnswer { get; }

        /// <summary>
        /// Milestone 8.35m's resolution focus: whether this card is dimmed because a DIFFERENT card in
        /// the same hand is <see cref="IsResolving"/> right now, so the eye lands on the one card whose
        /// text is running rather than wandering the rest of the hand. Only the ordinary hand's other
        /// Available cards ever carry this - never the resolving card itself, which shines instead of
        /// dimming, and never an offered candidate on a separate surface (a mid-resolution Defense
        /// choice, say), which is exactly the amendment's "shine and are not dimmed" exception,
        /// satisfied here by construction rather than by an extra check: that candidate is a different
        /// view model on a different surface, not a member of this hand at all.
        /// </summary>
        public bool IsDimmedByResolution { get; }

        /// <summary>
        /// Milestone 9.32g. p.10's facedown resolved or discarded card (Bushido), drawn to the seat that
        /// may read it: its own face, hatched (approved mockup B). Another seat never holds this view
        /// model for such a card - it holds a <see cref="WithheldCardViewModel"/>.
        /// </summary>
        public bool IsFacedown { get; }

        /// <summary>
        /// `RESOLVING`, `PLAYED · TURN n`, `AVAILABLE`, or `DISCARDED` - and never a round number.
        /// </summary>
        public string HistoryLabel { get; }
    }

    /// <summary>
    /// The one card a hero has in play and has not resolved, as the rail's upper-right marker draws it:
    /// a commitment nobody may read yet, or the revealed card whose turn is being resolved.
    ///
    /// Milestone 8.7a, and the separation the rail lacked. The four numbered slots are resolved turn
    /// history; a card that has been committed or revealed but has not resolved is not history, and
    /// putting it in a numbered slot said a turn was over while it was still being played.
    ///
    /// <para>Which of the two states it is in is the projected
    /// <see cref="TurnCardCommitmentStatus"/> itself rather than a second word invented beside it, so
    /// presentation cannot hold a different opinion about a commitment than the authority that recorded
    /// one. The facedown state carries no card at all - not a card drawn differently, no card - because
    /// the information boundary is what the type is for: until joint reveal the projection itself
    /// refuses to name the committed instance, and this shape means presentation could not leak it even
    /// from the director's complete definition list.</para>
    /// </summary>
    public sealed class HeroCurrentCardViewModel
    {
        public HeroCurrentCardViewModel(TurnCardCommitmentStatus status, TrayCardViewModel card = null,
            int? cardInitiative = null, int itemInitiativeBonus = 0)
        {
            if (status != TurnCardCommitmentStatus.CardCommittedFacedown &&
                status != TurnCardCommitmentStatus.Revealed)
            {
                throw new ArgumentException(
                    "A hero's current card is either committed facedown or revealed.", nameof(status));
            }

            if ((status == TurnCardCommitmentStatus.Revealed) != (card != null))
            {
                throw new ArgumentException(
                    "A revealed current card names its card, and only a revealed one does.",
                    nameof(card));
            }

            if (itemInitiativeBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemInitiativeBonus),
                    "p.16's item bonus raises a printed icon; it never lowers one.");
            }

            if (status == TurnCardCommitmentStatus.CardCommittedFacedown &&
                (cardInitiative.HasValue || itemInitiativeBonus != 0))
            {
                // The same boundary the null card above draws, on the numbers: a facedown marker is
                // built from a commitment's status alone, so there is nothing here to leak even by
                // arithmetic - a hero's item count is public, and a printed Initiative beside it names
                // the card.
                throw new ArgumentException(
                    "A facedown commitment carries no Initiative at all, printed or item.",
                    nameof(cardInitiative));
            }

            Status = status;
            Card = card;
            IsFacedown = status == TurnCardCommitmentStatus.CardCommittedFacedown;
            StateLabel = IsFacedown ? "FACEDOWN" : "RESOLVING";
            Color = card?.Color ?? CardColor.None;
            CardInitiative = cardInitiative;
            ItemInitiativeBonus = itemInitiativeBonus;
            CardInitiativeLabel = IsFacedown ? string.Empty :
                cardInitiative?.ToString(System.Globalization.CultureInfo.InvariantCulture) ??
                card?.Face?.Initiative?.AmountLabel ?? string.Empty;
            // Milestone 8.55, the user on their own capture of Swift resolving Reload! with one
            // Initiative item: "this should be noted as 6+1." 8.42a had summed the two into `7`, and a
            // player reading `7` cannot see either the number they committed or what their item added;
            // the two readings are kept apart here, and a card with no item still reads exactly `6`.
            InitiativeLabel = CardInitiativeLabel.Length == 0 || itemInitiativeBonus == 0
                ? CardInitiativeLabel
                : CardInitiativeLabel + "+" +
                    itemInitiativeBonus.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // "Card 6 + item 1", and plainly "Card 6" with no item: the reading a player cannot see
            // the badge's own `6+1` decomposition in says the same two numbers in words, where 8.42a
            // read out the sum it had already made ("Card + item Initiative: 7").
            var reading = itemInitiativeBonus == 0
                ? "Card " + CardInitiativeLabel
                : "Card " + CardInitiativeLabel + " + item " +
                    itemInitiativeBonus.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AssistiveLabel = IsFacedown
                ? "Current card: committed facedown, not revealed"
                : $"Current card: {card.DisplayName} - {card.ColorName} - {card.ValueLine} - " +
                    StateLabel + " - Initiative: " + reading;
        }

        /// <summary>The projected commitment this marker is a reading of.</summary>
        public TurnCardCommitmentStatus Status { get; }

        /// <summary>Whether the commitment is still facedown, so nothing about the card may be shown.</summary>
        public bool IsFacedown { get; }

        /// <summary>
        /// The revealed card, drawn by the same builder the tray and the history slots draw one with,
        /// or null while the commitment is facedown.
        /// </summary>
        public TrayCardViewModel Card { get; }

        /// <summary>The revealed card's printed colour, or <c>None</c> while it is facedown.</summary>
        public CardColor Color { get; }

        /// <summary>
        /// The revealed card's printed Initiative, as a number rather than as text, or null while the
        /// commitment is facedown. Milestone 8.55: the badge reads the two numbers apart, so the view
        /// model carries them apart, and a check can assert the reading against the arithmetic instead
        /// of against a string this type composed.
        /// </summary>
        public int? CardInitiative { get; }

        /// <summary>
        /// What p.16's Initiative items add to that printed number for this owner - zero when the hero
        /// holds none, and zero for a facedown commitment. Decided by the shared
        /// <see cref="ItemBonusQuery"/>, never by subtracting a printed total out of a sum.
        /// </summary>
        public int ItemInitiativeBonus { get; }

        /// <summary>The printed Initiative alone, as the badge's left-hand reading; empty facedown.</summary>
        public string CardInitiativeLabel { get; }

        /// <summary>
        /// Printed Initiative and p.16's item bonus as the rail draws them - `6+1` with one item, `6`
        /// with none, empty while facedown.
        ///
        /// <para>Milestone 8.55. 8.42a put the SUM here, and the user, mid-playtest, on Swift's plaque
        /// while Reload! resolved: "this should be noted as 6+1." A committed number the player chose
        /// and a bonus the game supplies are two facts, and `7` is neither of them.</para>
        /// </summary>
        public string InitiativeLabel { get; }

        /// <summary>`FACEDOWN` or `RESOLVING` - the word the marker carries beside its colour.</summary>
        public string StateLabel { get; }

        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// A public marker assigned to one numbered turn slot. The resource convention is generic: any
    /// future turn-slot marker can supply art under its definition id without teaching presentation
    /// a hero name or a rule.
    /// </summary>
    public sealed class TurnSlotMarkerViewModel
    {
        public TurnSlotMarkerViewModel(MarkerAttachment attachment)
        {
            if (attachment == null) throw new ArgumentNullException(nameof(attachment));
            if (!attachment.TurnSlotIndex.HasValue)
                throw new ArgumentException("A turn-slot marker names its numbered slot.", nameof(attachment));
            TurnSlotIndex = attachment.TurnSlotIndex.Value;
            DisplayName = attachment.Definition.DisplayName;
            ResourcePath = ResourcePathFor(attachment.Definition.Id);
            AssistiveLabel = DisplayName + " assigned to turn " +
                TurnSlotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public int TurnSlotIndex { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public string AssistiveLabel { get; }

        public static string ResourcePathFor(MarkerDefinitionId definitionId) =>
            "Private/Markers/" + definitionId.Value;

        /// <summary>
        /// The glyph a <c>RuneTurnSlot</c> choice's ordinal names: the same marker definition the
        /// content's handler places for that ordinal, so the pop-up piece and the tray agree.
        /// </summary>
        public static string RuneGlyphResourcePath(int runeOrdinal) =>
            runeOrdinal < 1 || runeOrdinal > GuardsDigital.Core.Cards.Verified.SnorriComponentContent.Runes.Length
                ? null
                : ResourcePathFor(GuardsDigital.Core.Cards.Verified.SnorriComponentContent
                    .Runes[runeOrdinal - 1].Id);
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
            Symbol = MatchHudTheme.ChannelSymbol(channel);
            AmountLabel = MatchHudTheme.Signed(amount);
            AssistiveLabel = $"{MatchHudTheme.ChannelName(channel)} item bonus {AmountLabel}";
        }

        public PrintedValueChannel Channel { get; }
        public int Amount { get; }
        public string Token { get; }
        public PrintedChannelSymbolDefinition Symbol { get; }
        public string AmountLabel { get; }
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// One rulebook p.18 marker this hero is holding, and what holding it does to them.
    ///
    /// <para>Milestone 8.23a, and it is deliberately the same shape as
    /// <see cref="HeroItemBonusViewModel"/> above rather than a new idiom. p.16's item and p.18's
    /// marker are the same kind of fact about a hero - a named component that shifts named value
    /// channels by named amounts - and they differ in the sign and in who gave it. Drawing them two
    /// different ways would have made a player learn two readings of one thing.</para>
    ///
    /// <para><b>Every part of it is the definition's.</b> The name is the marker's own display name,
    /// and each chip is one <c>MarkerPropertyDefinition</c>'s declared channel and amount. Nothing is
    /// summed, nothing is negated here, and nothing is looked up by hero: Tigerclaw's two Poison
    /// strengths print -1 and -2 on their own cards and arrive here already carrying their sign, so a
    /// third strength added by a later hero draws itself with no change to this type.</para>
    ///
    /// <para>A property that declares no value modification contributes no chip. It is a real case -
    /// <c>MarkerPropertyDefinition.Modification</c> is nullable for a property that only declares its
    /// source and lifetime - and a chip reading `- 0` would be this surface inventing a rule the
    /// component does not state.</para>
    /// </summary>
    public sealed class HeroMarkerViewModel
    {
        public HeroMarkerViewModel(MarkerAttachment attachment)
        {
            if (attachment == null) throw new ArgumentNullException(nameof(attachment));

            MarkerInstanceId = attachment.Id;
            HolderHeroId = attachment.HolderHeroId;
            DisplayName = attachment.Definition.DisplayName;
            NameLabel = DisplayName.ToUpperInvariant();
            ValueChanges = Array.AsReadOnly(attachment.Definition.Properties
                .Where(property => property.Modification != null &&
                    property.Modification.Channel != PrintedValueChannel.None)
                .Select(property => new HeroMarkerValueChangeViewModel(
                    property.Modification.Channel, property.Modification.Amount))
                .ToArray());

            // `Poison: Initiative -1, Attack -1, Defense -1`, or `Poison` on its own for a marker
            // whose properties change no value. The pointer and an assistive reader get the whole
            // sentence, on the terms every other compact rail reading is named by.
            AssistiveLabel = ValueChanges.Count == 0
                ? DisplayName
                : DisplayName + ": " + string.Join(
                    ", ", ValueChanges.Select(change => change.AssistiveLabel));
        }

        public MarkerInstanceId MarkerInstanceId { get; }

        /// <summary>The hero this marker is on, which is the hero whose values it changes.</summary>
        public HeroId HolderHeroId { get; }

        public string DisplayName { get; }

        /// <summary>`POISON` - the marker's own name, in the rail's caption case.</summary>
        public string NameLabel { get; }

        /// <summary>One chip per channel the marker's properties change, in declaration order.</summary>
        public IReadOnlyList<HeroMarkerValueChangeViewModel> ValueChanges { get; }

        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// One value channel a held marker changes, as a signed chip.
    ///
    /// Milestone 8.23a, and the twin of <see cref="HeroItemBonusViewModel"/> - same token, same signed
    /// amount, same words. It is a separate type only because an item bonus asserts its amount is
    /// positive, which is true of p.16's items and false of the thing this draws.
    /// </summary>
    public sealed class HeroMarkerValueChangeViewModel
    {
        public HeroMarkerValueChangeViewModel(PrintedValueChannel channel, int amount)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A marker value change names its channel.", nameof(channel));
            }

            if (amount == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), "A change of nothing is not a change; such a property draws no chip.");
            }

            Channel = channel;
            Amount = amount;
            Token = MatchHudTheme.ChannelToken(channel);
            AmountLabel = MatchHudTheme.Signed(amount);
            AssistiveLabel = $"{MatchHudTheme.ChannelName(channel)} {AmountLabel}";
        }

        public PrintedValueChannel Channel { get; }
        public int Amount { get; }
        public string Token { get; }
        public string AmountLabel { get; }
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// One of the four fixed slots standing for this hero's <em>resolved</em> turns in the current
    /// round.
    ///
    /// Milestone 8.7a made both of those words true. A slot is occupied only once the card played on
    /// its turn has reached <see cref="CardResolutionStatus.Resolved"/>.
    ///
    /// <para>Milestone 8.11b took the ordinal off it. The four slots keep fixed left-to-right positions
    /// under one `RESOLVED TURNS` caption, so the sequence is still on the screen; what the slot draws
    /// instead of `1` is the card that resolved on that turn, as a compact card-shaped icon. A number
    /// standing where a card belongs was the abbreviation this milestone removes - the row already said
    /// which turn was which by where a slot sits, and said nothing about what was played there.
    /// <see cref="TurnIndex"/> is still the fact the sequence and <see cref="AssistiveLabel"/> are built
    /// from; it simply has no text of its own any more.</para>
    /// </summary>
    public sealed class HeroTurnSlotViewModel
    {
        public HeroTurnSlotViewModel(
            int turnIndex,
            CardColor color,
            bool isOccupied,
            TrayCardViewModel playedCard = null,
            TurnSlotMarkerViewModel turnSlotMarker = null,
            bool isWithheldFacedown = false)
        {
            if (turnIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            // Milestone 9.32g. A facedown card this seat may not name occupies its slot with no card,
            // known only by its colour (mockup B: every seat sees the hatched gold).
            if (isWithheldFacedown && (!isOccupied || playedCard != null || color == CardColor.None))
            {
                throw new ArgumentException(
                    "A withheld facedown slot is occupied, names no card and carries its known colour.",
                    nameof(isWithheldFacedown));
            }

            if (!isWithheldFacedown && isOccupied != (playedCard != null))
            {
                throw new ArgumentException(
                    "An occupied slot names the card played on its turn, and only an occupied one does.",
                    nameof(playedCard));
            }

            IsWithheld = isWithheldFacedown;

            TurnIndex = turnIndex;
            if (turnSlotMarker != null && turnSlotMarker.TurnSlotIndex != turnIndex)
                throw new ArgumentException("A slot can only draw the marker assigned to it.", nameof(turnSlotMarker));
            TurnSlotMarker = turnSlotMarker;
            Color = color;
            IsOccupied = isOccupied;
            PlayedCard = playedCard;
            var ordinal = turnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Milestone 8.7a: the same projected active-source fact the tray reads, asked of the card
            // in the slot rather than derived again here, so the two surfaces mark and unmark one card
            // on the same refresh.
            IsActiveEffectSource = isOccupied && playedCard != null && playedCard.IsActiveEffectSource;
            ActiveLabel = IsActiveEffectSource ? "ACTIVE" : string.Empty;
            // Milestone 8.11b: the slot draws no text at all now, so this is where the turn ordinal and
            // the card's name live. A player using a screen reader hears more than the four numbers
            // ever said, and a sighted player reads the same facts off the icon's own hover.
            AssistiveLabel = isWithheldFacedown
                ? $"Turn {ordinal}: {MatchHudTheme.ColorName(color)} card - facedown"
                : isOccupied
                ? $"Turn {ordinal}: {MatchHudTheme.ColorName(color)} card - {playedCard.DisplayName}" +
                    (IsActiveEffectSource ? " - ACTIVE EFFECT IN EFFECT" : string.Empty) +
                    (playedCard.IsFacedown ? " - FACEDOWN" : string.Empty)
                : $"Turn {ordinal}: empty";
            if (TurnSlotMarker != null)
                AssistiveLabel += " - " + TurnSlotMarker.DisplayName;
        }

        public int TurnIndex { get; }

        public CardColor Color { get; }
        public bool IsOccupied { get; }

        /// <summary>
        /// Milestone 8.7a: whether the card resolved on this turn is still sourcing a live Active
        /// effect, so the resolved slot wears milestone 8.5b's outline and word for exactly as long as
        /// the tray card does.
        /// </summary>
        public bool IsActiveEffectSource { get; }

        /// <summary>`ACTIVE`, or empty when nothing this slot's card sourced is live.</summary>
        public string ActiveLabel { get; }

        /// <summary>
        /// The card this hero played on that turn, as the tray draws it, or null for an empty slot.
        ///
        /// Milestone 8.5a, by the user's 2026-08-21 decision: resting the pointer on an occupied slot
        /// reveals that card's whole printed face. The slot is the only place a round's earlier turns
        /// stay visible once the tray has moved on, and until this it carried a swatch and a word.
        /// </summary>
        public TrayCardViewModel PlayedCard { get; }

        public TurnSlotMarkerViewModel TurnSlotMarker { get; }

        public string AssistiveLabel { get; }

        /// <summary>Milestone 9.32g. A facedown card is here and this seat may not name it.</summary>
        public bool IsWithheld { get; }

        /// <summary>Milestone 9.32g. The slot's card lies facedown, whether this seat may read it or not.</summary>
        public bool IsFacedown => IsWithheld || (PlayedCard != null && PlayedCard.IsFacedown);
    }

    /// <summary>
    /// Milestone 9.32g. A facedown card another seat may not name - in a turn slot or a discard pile -
    /// drawn as the hatched mini of its known colour, its hover a card back (approved mockup B, as
    /// amended on 2026-10-02: "they just cannot see what's on it").
    /// </summary>
    public sealed class WithheldCardViewModel
    {
        public WithheldCardViewModel(HeroContentId ownerContentId, CardColor color, string assistiveLabel)
        {
            OwnerContentId = ownerContentId;
            Color = color;
            AssistiveLabel = assistiveLabel ?? throw new ArgumentNullException(nameof(assistiveLabel));
        }

        public HeroContentId OwnerContentId { get; }
        public CardColor Color { get; }
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
            HeroContentId contentId,
            string heroDisplayName,
            int level,
            int coins,
            IEnumerable<HeroItemBonusViewModel> itemBonuses,
            IEnumerable<HeroTurnSlotViewModel> turnSlots,
            IEnumerable<TrayCardViewModel> discardCards,
            bool isViewed,
            HeroCurrentCardViewModel currentCard = null,
            TrayCardViewModel ultimateCard = null,
            IEnumerable<HeroMarkerViewModel> heldMarkers = null,
            bool isAwaited = false,
            int worth = 0,
            TurnSlotMarkerViewModel currentTurnMarker = null,
            SpellbookViewModel spellbook = null,
            GuardsDigital.Core.Rules.Victory.TeamVictoryTally? ultimateTally = null,
            IEnumerable<CardColor> withheldDiscardColors = null,
            bool worthRepresentsAssistCoinsOnly = false)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("An overview names its hero.", nameof(heroId));
            }

            WithheldDiscards = Array.AsReadOnly((withheldDiscardColors ?? Enumerable.Empty<CardColor>())
                .Select(color => new WithheldCardViewModel(
                    contentId, color, $"{MatchHudTheme.ColorName(color)} card - facedown - DISCARDED"))
                .ToArray());

            if (spellbook != null && !spellbook.HeroId.Equals(heroId))
            {
                throw new ArgumentException("A rail draws its own hero's spellbook.", nameof(spellbook));
            }

            Spellbook = spellbook;

            if (worth < 0 || worth > 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worth), worth, "p.13's table prices a hero at 1, 2 or 3, or not at all.");
            }

            if (coins < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coins), coins, "A projected coin balance is never negative.");
            }

            HeroId = heroId;
            TeamSide = teamSide;
            ContentId = contentId;
            HeroDisplayName = heroDisplayName ?? throw new ArgumentNullException(nameof(heroDisplayName));
            Level = level;
            Coins = coins;
            ItemBonuses = Array.AsReadOnly((itemBonuses ??
                Enumerable.Empty<HeroItemBonusViewModel>()).ToArray());
            TurnSlots = Array.AsReadOnly((turnSlots ??
                throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            DiscardCards = Array.AsReadOnly((discardCards ??
                Enumerable.Empty<TrayCardViewModel>()).ToArray());
            IsViewed = isViewed;
            IsAwaited = isAwaited;
            CurrentCard = currentCard;
            UltimateCard = ultimateCard;

            // Milestone 9.30g (approved mockup D): "after Tier IV his Wish count reads 1/3 beside the
            // purple card" - drawn only beside an unlocked Ultimate, and only once the team counts.
            UltimateTallyLabel = ultimateCard != null && ultimateTally.HasValue
                ? ultimateTally.Value.ToString()
                : null;
            UltimateTallyAssistiveLabel = UltimateTallyLabel == null
                ? null
                : $"{ultimateTally.Value.Count} of {ultimateTally.Value.Threshold} toward the team's card victory";
            CurrentTurnMarker = currentTurnMarker;
            HeldMarkers = Array.AsReadOnly((heldMarkers ??
                Enumerable.Empty<HeroMarkerViewModel>()).ToArray());
            if (HeldMarkers.Any(marker => !marker.HolderHeroId.Equals(heroId)))
            {
                throw new ArgumentException(
                    "A rail draws the markers its own hero holds; a marker on somebody else belongs on their rail.",
                    nameof(heldMarkers));
            }

            TeamLabel = teamSide == TeamSide.Red ? "RED HERO" : "BLUE HERO";
            LevelLabel = "LVL " + level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Milestone 8.49. What defeating this hero is worth, next to the Level it follows: p.13's
            // assist coins to every friendly hero of the attacker AND the Life counters the defeated
            // team spends are one number, the hero's lowest card Tier read as 1, 2 or 3 - which is
            // why the badge is half a coin and half a heart. The user asked for it at the table.
            Worth = worth;
            WorthRepresentsAssistCoinsOnly = worthRepresentsAssistCoinsOnly;
            WorthLabel = worth == 0
                ? string.Empty
                : worth.ToString(System.Globalization.CultureInfo.InvariantCulture);
            WorthAssistiveLabel = worth == 0
                ? string.Empty
                : worthRepresentsAssistCoinsOnly
                    ? $"Worth {worth}: defeating this hero pays {worth} assist coin{(worth == 1 ? string.Empty : "s")}"
                    : $"Worth {worth}: defeating this hero pays {worth} assist coin{(worth == 1 ? string.Empty : "s")} " +
                        $"and costs their team {worth} life counter{(worth == 1 ? string.Empty : "s")}";
            CoinAmountLabel = coins.ToString(System.Globalization.CultureInfo.InvariantCulture);
            CoinAssistiveLabel = coins == 1 ? "1 coin" : CoinAmountLabel + " coins";
            DiscardLabel = "DISCARD";

            // Milestone 8.25c. The image slot the milestone 7.8a decision wired is now filled: this is
            // where the hero's own official token face lives, addressed exactly as the piece on the
            // board is - by projected content identity and authoritative team, never by the display
            // name beside it and never by anything read out of the picture. Until this milestone the
            // disc carried the first letter of the display name instead, which told six heroes apart
            // only as well as their initials happen to differ.
            PortraitResourcePath = contentId.IsDefined
                ? HeroTokenArtwork.ResourcePath(contentId, teamSide)
                : null;
            PortraitAssistiveLabel = HeroDisplayName + ", " + TeamLabel.ToLowerInvariant();
        }

        public HeroId HeroId { get; }
        public TeamSide TeamSide { get; }

        public TurnSlotMarkerViewModel CurrentTurnMarker { get; }

        /// <summary>
        /// Which hero's printed set this rail is drawing - the same identity
        /// <c>QuickGameHeroSetup.ContentId</c> states and the board's own piece is addressed by.
        /// Milestone 8.25c: the rail's portrait is selected from this, so the picture follows the hero
        /// the projection names rather than the words drawn beside it.
        /// </summary>
        public HeroContentId ContentId { get; }

        /// <summary>
        /// Candidate team-baked portrait path, or null when the projection names no content identity.
        ///
        /// <para>The Unity view resolves baked/neutral availability with the board's adapter - see
        /// <see cref="HeroTokenArtwork"/> - which is what makes the rail's picture and the piece on the
        /// map the same hero rather than two artists' opinions of one. The artwork is private and a
        /// clean checkout has none, so the path resolving to nothing is an ordinary state; the rail
        /// draws the team-accented disc with no letter in it, and the hero's written name beside it is
        /// what carries the identity there.</para>
        /// </summary>
        public string PortraitResourcePath { get; }

        /// <summary>
        /// What the portrait reads as to a hover and to assistive technology: the hero's name and their
        /// team, in words. Milestone 8.25c keeps the readable identity off the picture, for the reason
        /// the disc's team reading was kept off it at 8.7a - a surface whose only statement is an image
        /// or a colour states nothing to a reader who cannot use either.
        /// </summary>
        public string PortraitAssistiveLabel { get; }

        public string HeroDisplayName { get; }
        /// <summary>
        /// `RED HERO` or `BLUE HERO`. Milestone 8.7a took it off the rail's face by the user's
        /// 2026-08-21 decision - two rails on two edges in two accent colours already say which side a
        /// hero is on, and the line repeated it a third time in the header the current-card marker
        /// needed - and left it on the hero disc's own hover and assistive reading, which is where
        /// milestone 8.5a put a card's printed colour for the same reason.
        /// </summary>
        public string TeamLabel { get; }

        /// <summary>p.15's Hero Level, projected rather than counted from upgrades seen.</summary>
        public int Level { get; }

        public string LevelLabel { get; }

        /// <summary>
        /// Milestone 8.49. p.13's price on this hero - its lowest card Tier read as 1, 2 or 3, the
        /// assist coins a Defeat pays and the Life counters it costs - or zero when the hero has no
        /// Tier to read. Computed by <see cref="GuardsDigital.Core.Rules.Consequences.HeroDefeatCostQuery"/>, the same query the Defeat is
        /// priced through, so the badge and the charge cannot disagree.
        /// </summary>
        public int Worth { get; }
        public bool WorthRepresentsAssistCoinsOnly { get; }

        /// <summary>The digit drawn inside the half-coin, half-heart badge; empty when there is none.</summary>
        public string WorthLabel { get; }

        /// <summary>What the badge reads as to a hover and to assistive technology.</summary>
        public string WorthAssistiveLabel { get; }

        /// <summary>
        /// What this hero has to spend right now, from the one ledger rulebook p.13's Defeat reward,
        /// p.8's Pity Coin, a card effect's grant and p.15's Level Up all move.
        ///
        /// <para>Milestone 8.21a. It is <c>HeroProgressionProjection.Coins</c> read straight through -
        /// never a running total this presenter keeps, never a sum of coin events it watched - which is
        /// the whole reason the count survives a snapshot restore, a deterministic replay and a
        /// retained-prefix Undo without a presentation-owned lifetime and without a new serialized
        /// field: every one of those paths rebuilds the ledger it already preserved, and the same
        /// number falls out.</para>
        ///
        /// <para>It is public match information and is drawn unconditionally, whether or not p.15's
        /// Upgrade a Card or any other coin-related decision currently stands. A balance a player can
        /// see only while they are being asked to spend it is visible at the one moment it is too late
        /// to plan with.</para>
        /// </summary>
        public int Coins { get; }

        /// <summary>The balance itself, which is the only part of the readout that changes.</summary>
        public string CoinAmountLabel { get; }

        /// <summary>
        /// `3 coins`, or `1 coin` - the same fact in words, for the hover and assistive reading.
        /// </summary>
        public string CoinAssistiveLabel { get; }

        public IReadOnlyList<HeroItemBonusViewModel> ItemBonuses { get; }
        public IReadOnlyList<HeroTurnSlotViewModel> TurnSlots { get; }
        public string DiscardLabel { get; }

        /// <summary>
        /// Every card currently in this hero's discard pile, as the tray draws one, in the order
        /// <see cref="CurrentRoundCards.DiscardedCards"/> settles.
        ///
        /// <para>Milestone 8.11b. The rail carried a count and a strip of colour pips, which said how
        /// big the pile was and which colours were somewhere in it, and could not say what was in it -
        /// two of one colour read as one pip. One card, one icon says both, and says them the same way
        /// the resolved-turn slots do. <see cref="DiscardCount"/> is what this list is long, never a
        /// number drawn beside it: the visible icons are the count.</para>
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> DiscardCards { get; }

        /// <summary>
        /// Milestone 9.32g. The facedown cards in this hero's pile that this seat may not name, by known
        /// colour, drawn after the named ones (mockup B).
        /// </summary>
        public IReadOnlyList<WithheldCardViewModel> WithheldDiscards { get; }

        /// <summary>How many cards are in the pile. It is the list's own length, and it is not drawn.</summary>
        public int DiscardCount => DiscardCards.Count + WithheldDiscards.Count;

        /// <summary>Whether the tray is currently showing this hero, which the rail marks.</summary>
        public bool IsViewed { get; }

        /// <summary>
        /// Milestone 8.35m (the SEVENTH amendment's tray shine). Whether the match is currently waiting
        /// on this hero's own input - <see cref="AwaitedInputQuery.IsHeroAwaited"/>, the same predicate
        /// the top bar reads at viewer scope, read here at hero scope so several trays can shine at
        /// once during a commitment phase and the rail never has to guess whose turn it is a second
        /// way. See that method for exactly which awaited cases this covers today and which four remain
        /// a deliberate non-goal.
        /// </summary>
        public bool IsAwaited { get; }

        /// <summary>
        /// Milestone 8.7a: the card this hero has in play and has not resolved, or null when they have
        /// none - awaiting a commitment, passed, or already resolved this turn's card.
        /// </summary>
        public HeroCurrentCardViewModel CurrentCard { get; }

        /// <summary>
        /// Milestone 8.15: the card faceup in rulebook p.16's Ultimate area, or null while the hero
        /// has not unlocked one. It is a reading of the card's own projected location - the unlock
        /// <em>is</em> the position - so a restore, a replay and a retained-prefix Undo redraw it
        /// with no presentation-owned state.
        /// </summary>
        public TrayCardViewModel UltimateCard { get; }

        /// <summary>
        /// Milestone 9.30g. "n/3" - this hero's team's count toward a printed team victory (Gydion's
        /// Wish) over its printed threshold, drawn beside <see cref="UltimateCard"/>; null while there
        /// is no unlocked Ultimate or the team has not counted. It is
        /// <c>IMatchReadModel.TeamVictoryTallies</c> read straight through, on the terms
        /// <see cref="Coins"/> is, so it survives a restore, a replay and an Undo with no state here.
        /// </summary>
        public string UltimateTallyLabel { get; }

        /// <summary>The same count in words, for the hover and assistive reading.</summary>
        public string UltimateTallyAssistiveLabel { get; }

        /// <summary>
        /// Milestone 8.23a: rulebook p.18's markers this hero is currently holding, and what each one
        /// does to them.
        ///
        /// <para>It is <c>IMatchReadModel.MarkersHeldBy</c> read straight through, on exactly
        /// the terms milestone 8.21a put <see cref="Coins"/> here: never a set this presenter
        /// maintains, never a tally of give-and-return events it watched. That is what makes the
        /// reading survive a snapshot restore, a deterministic replay, a retained-prefix Undo and a
        /// presentation rebuild without a lifetime of its own - every one of those paths already
        /// reconstructs the attachment register, and the same markers fall out of it.</para>
        ///
        /// <para><b>It is the holder's rail, which is normally the opponent's.</b> p.18 describes
        /// markers as components "usually given to other heroes", so Tigerclaw poisoning an enemy puts
        /// this reading on the enemy's rail and not on his. That is the point of the milestone rather
        /// than an accident of it: a rule lowering three of a hero's values has to be visible to the
        /// player it is applied to.</para>
        ///
        /// <para>Usually empty, and drawn only when it is not. p.8 returns every marker at the end of
        /// every round, so most of a match has no row here at all - which is the same conditional the
        /// <see cref="UltimateCard"/> above is drawn under, in the item strip.</para>
        /// </summary>
        public IReadOnlyList<HeroMarkerViewModel> HeldMarkers { get; }

        /// <summary>`MARKERS`, the held-marker row's caption.</summary>
        public string MarkersLabel => "MARKERS";

        /// <summary>
        /// Milestone 9.30h: this hero's spellbook as this seat's projection shows it - the items
        /// strip's "SPELLBOOK n | m" chip and the drawer it opens (approved mockups B to D) - or null
        /// for a hero who owns no spell, which is every hero but a spellbook's owner.
        /// </summary>
        public SpellbookViewModel Spellbook { get; }
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
        private readonly IMatchClient session;
        private readonly IHeroOverviewsView view;
        private readonly Func<HeroId> viewedHero;
        private readonly EffectCapabilityCatalog activeEffectCapabilities;
        private IMatchReadModel lastProjection;
        private HeroId lastViewedHero;
        private HeroOverviewsViewModel lastViewModel;

        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor, composing the Director's own client.
        /// </summary>
        public HeroOverviewsPresenter(
            IMatchSession session,
            IHeroOverviewsView view,
            Func<HeroId> viewedHero = null,
            EffectCapabilityCatalog activeEffectCapabilities = null)
            : this(new DirectorMatchClient(session), view, viewedHero, activeEffectCapabilities)
        {
        }

        /// <param name="activeEffectCapabilities">
        /// Milestone 8.61h. The catalog this composition was given, threaded to the Initiative badge
        /// so it can read p.16's item bonus as the rules do while Min's "This round: Double your item
        /// bonuses" stands. Optional and defaulting to none, on
        /// <c>BoardInteractionPresenter</c>'s own precedent (milestone 8.35z63): a composition naming
        /// no catalog reads the page unmodified rather than reaching for the build's.
        /// </param>
        public HeroOverviewsPresenter(
            IMatchClient session,
            IHeroOverviewsView view,
            Func<HeroId> viewedHero = null,
            EffectCapabilityCatalog activeEffectCapabilities = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.viewedHero = viewedHero ?? (() => default);
            this.activeEffectCapabilities = activeEffectCapabilities;
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
                var worthRepresentsAssistCoinsOnly =
                    ((projection as IExperimentalReadModel)?.Experiments ??
                        (session as IExperimentalMatchClient)?.Experiments ??
                        ExperimentalMatchOptions.Disabled).HeroDefeatBaseLifeCostIsOne;
                lastViewModel = new HeroOverviewsViewModel(
                    projection.Version,
                    projection.Setup.Heroes
                        .OrderBy(hero => hero.TeamSide)
                        .ThenBy(hero => PlayerVisualIdentity.Index(projection.Participants, hero.HeroId))
                        .Select(hero => Build(
                            projection,
                            activeEffectCapabilities,
                            hero.HeroId,
                            hero.TeamSide,
                            // Milestone 8.25c: the rail's portrait is addressed by this, so it comes
                            // off the projected setup beside the hero's seat and team rather than out
                            // of the name the rail happens to print.
                            hero.ContentId,
                            hero.DisplayName,
                            viewed,
                            worthRepresentsAssistCoinsOnly)));
            }

            view.Render(lastViewModel);
        }

        private static HeroOverviewViewModel Build(
            IMatchReadModel projection,
            EffectCapabilityCatalog activeEffectCapabilities,
            HeroId heroId,
            TeamSide teamSide,
            HeroContentId contentId,
            string displayName,
            HeroId viewedHeroId,
            bool worthRepresentsAssistCoinsOnly)
        {
            var progression = projection.HeroProgression
                .FirstOrDefault(entry => entry.HeroId.Equals(heroId));

            // Milestone 9.30h: null for a hero who owns no spellbook (no book count, no faceup spell).
            var spellbook = SpellbookPresentation.ForHero(projection, heroId, displayName);
            return new HeroOverviewViewModel(
                heroId,
                teamSide,
                contentId,
                displayName,
                progression?.Level ?? 0,
                progression?.Coins ?? 0,
                (progression?.ItemBonuses ?? Array.Empty<KeyValuePair<PrintedValueChannel, int>>())
                    .OrderBy(entry => MatchHudTheme.ChannelOrder.ToList().IndexOf(entry.Key))
                    .Select(entry => new HeroItemBonusViewModel(entry.Key, entry.Value)),
                CurrentRoundCards.TurnSlots(projection, heroId),
                CurrentRoundCards.DiscardedCards(projection, heroId),
                heroId.Equals(viewedHeroId),
                CurrentRoundCards.CurrentCard(projection, heroId, activeEffectCapabilities),
                CurrentRoundCards.UnlockedUltimate(projection, heroId),
                // Milestone 8.23a. The register's own order, kept rather than sorted: markers are
                // given one at a time and the order they were given in is the only order there is.
                projection.MarkersHeldBy(heroId)
                    // Milestone 8.93f: a marker assigned to a numbered turn slot is drawn in that
                    // slot, not repeated in the held list.
                    .Where(marker => !marker.TurnSlotIndex.HasValue)
                    .Select(marker => new HeroMarkerViewModel(marker)),
                AwaitedInputQuery.IsHeroAwaited(projection, heroId),
                // Milestone 8.49. The same reading the Defeat is priced by, over the same cards.
                GuardsDigital.Core.Rules.Consequences.HeroDefeatCostQuery.Worth(
                    heroId, projection.Definitions, projection.Cards),
                CurrentRoundCards.TurnSlotMarker(
                    projection, heroId, projection.TurnIndex),
                // Milestone 9.30h. Read off this seat's own projection, so privacy is the projection's.
                spellbook,
                // Milestone 9.30g. The team's public Wish count, beside the purple card of the hero
                // who casts the Wishes - the one who owns a spellbook, by the predicate the SPELLBOOK
                // chip is drawn under - and never beside a teammate's own Ultimate, where it would
                // read as that card's.
                spellbook != null
                    ? GuardsDigital.Core.Rules.Victory.TeamVictoryTallyQuery.ForHero(
                        projection.Participants, projection.TeamVictoryTallies, heroId)
                    : null,
                CurrentRoundCards.WithheldDiscardColors(projection, heroId),
                worthRepresentsAssistCoinsOnly);
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
        public static CardColor ColorOf(IMatchReadModel projection, CardInstance card)
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
            IMatchReadModel projection,
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
        /// not reached, one this hero passed on, and one whose card is still being resolved are all an
        /// empty slot rather than a missing one.
        ///
        /// <para>Milestone 8.7a: the slot fills on the card's own
        /// <see cref="CardResolutionStatus.Resolved"/> rather than on the assignment existing. The
        /// round records its assignment at the moment a turn starts - which is the same command as
        /// joint reveal - so until now the four slots occupied themselves the instant a card was
        /// revealed and presented a turn as history while it was still being played. Nothing here
        /// remembers a resolution: it is asked of the same card state the authority owns, so restore,
        /// replay and a retained-prefix Undo answer it without a presentation-side lifetime.</para>
        /// </summary>
        public static IReadOnlyList<HeroTurnSlotViewModel> TurnSlots(
            IMatchReadModel projection,
            HeroId heroId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var slots = new List<HeroTurnSlotViewModel>(RoundResolutionAuthority.TurnsPerRound);
            for (var turnIndex = 1; turnIndex <= RoundResolutionAuthority.TurnsPerRound; turnIndex++)
            {
                var turnSlotMarker = TurnSlotMarker(projection, heroId, turnIndex);
                var assignments = projection.TurnSlots
                    .Where(slot => slot.RoundIndex == projection.RoundIndex && slot.TurnIndex == turnIndex)
                    .SelectMany(slot => slot.Assignments)
                    .Where(value => value.HeroId.Equals(heroId))
                    .ToArray();

                // Milestone 9.32g. A facedown card this seat may not name: the slot is occupied by a card
                // of its known colour (every seat sees the hatched gold, mockup B).
                var withheld = assignments.FirstOrDefault(value => value.IsWithheld);
                if (withheld != null && withheld.KnownColor != CardColor.None)
                {
                    slots.Add(new HeroTurnSlotViewModel(
                        turnIndex, withheld.KnownColor, true, null, turnSlotMarker, isWithheldFacedown: true));
                    continue;
                }

                var assignment = assignments.FirstOrDefault(value => value.CardInstanceId.HasValue);
                var card = assignment == null
                    ? null
                    : projection.Cards.FirstOrDefault(value =>
                        value.Id.Equals(assignment.CardInstanceId.Value));
                slots.Add(card == null ||
                    card.State.ResolutionStatus != CardResolutionStatus.Resolved
                    ? new HeroTurnSlotViewModel(
                        turnIndex, CardColor.None, false, null, turnSlotMarker)
                    : new HeroTurnSlotViewModel(
                        turnIndex,
                        ColorOf(projection, card),
                        true,
                        TrayCard(projection, card, TrayCardGroup.Played, turnIndex, false, false),
                        turnSlotMarker));
            }

            return Array.AsReadOnly(slots.ToArray());
        }

        public static TurnSlotMarkerViewModel TurnSlotMarker(
            IMatchReadModel projection, HeroId heroId, int turnIndex)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (turnIndex < 1 || turnIndex > RoundResolutionAuthority.TurnsPerRound)
                return null;
            var marker = projection.MarkersHeldBy(heroId).SingleOrDefault(value =>
                value.TurnSlotIndex == turnIndex);
            return marker == null ? null : new TurnSlotMarkerViewModel(marker);
        }

        /// <summary>
        /// Every card in one hero's discard pile, as the tray draws one, in a deterministic order.
        ///
        /// <para>Milestone 8.11b. The rail draws one icon per card now, so the pile needs an order and
        /// the pile itself has none - p.6 says nothing about how a discard pile is stacked, and the
        /// projection records no arrival sequence. It is ordered by card instance identity, which is
        /// exactly what the tray already orders its own discarded group by, so a card sits in the same
        /// place in the pile on both surfaces. Reading the order off discard <em>events</em> as they
        /// went past would have been presentation maintaining state the authority does not keep, and it
        /// would not survive a restore or a replay; this survives both because it is a reading of the
        /// projection alone.</para>
        /// </summary>
        /// <summary>
        /// Milestone 9.32g. The colours of the facedown cards in this hero's pile that this seat's
        /// projection withholds: the table-public summary's facedown discards beyond the ones the seat
        /// holds as cards (its own), so the owner's seat never draws one twice.
        /// </summary>
        public static IReadOnlyList<CardColor> WithheldDiscardColors(IMatchReadModel projection, HeroId heroId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var summary = projection.HandSummaries?.FirstOrDefault(value => value.HeroId.Equals(heroId));
            if (summary == null || summary.FacedownDiscardColors.Count == 0)
            {
                return Array.Empty<CardColor>();
            }

            var held = projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.DiscardPile &&
                    CardLifecycleAuthority.IsFacedownRestingCard(card))
                .Select(card => CurrentRoundCards.ColorOf(projection, card))
                .ToList();
            var withheld = new List<CardColor>();
            foreach (var color in summary.FacedownDiscardColors)
            {
                if (!held.Remove(color))
                {
                    withheld.Add(color);
                }
            }

            return withheld.AsReadOnly();
        }

        public static IReadOnlyList<TrayCardViewModel> DiscardedCards(
            IMatchReadModel projection,
            HeroId heroId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return Array.AsReadOnly(projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .Select(card => TrayCard(projection, card, TrayCardGroup.Discarded, 0, false, false))
                .ToArray());
        }

        /// <summary>
        /// The card faceup in rulebook p.16's Ultimate area for this hero, or null while none is.
        ///
        /// <para>Milestone 8.15. The unlock is the card's own position - milestone 7.3's deliberate
        /// modelling - so this is a location read exactly like <see cref="DiscardedCards"/>, and the
        /// serializer already refuses an area p.16 could not have produced, which is what makes one
        /// card the most this can find.</para>
        /// </summary>
        public static TrayCardViewModel UnlockedUltimate(
            IMatchReadModel projection,
            HeroId heroId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var card = projection.Cards.FirstOrDefault(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.UltimateArea);
            return card == null
                ? null
                : TrayCard(projection, card, TrayCardGroup.Unlocked, 0, false, false);
        }

        /// <summary>
        /// The card this hero has in play and has not resolved: the facedown commitment of a turn that
        /// has not been revealed, or the revealed card whose turn is still being resolved. Null when
        /// the hero has nothing in play - awaiting a commitment, passed, or resolved.
        ///
        /// <para>Milestone 8.7a. The facedown case is the reason the answer is taken from the
        /// commitment rather than by looking for a card in the turn area: until joint reveal the
        /// authority's own projection refuses to name the committed instance, and reading a location to
        /// find it would be presentation deciding to know something the rules are withholding. What is
        /// left after that refusal is exactly a status, which is all the facedown marker draws.</para>
        /// </summary>
        /// <param name="activeEffectCapabilities">
        /// Milestone 8.61h. The catalog this composition was given, so the badge's item reading can
        /// ask whether a live Active effect of this hero's doubles p.16's bonus - Min the Dragonmonk's
        /// "This round: Double your item bonuses", which makes one item read `+2`.
        ///
        /// <para>Optional and defaulting to none, on milestone 8.35z63's own precedent for the aura
        /// derivation: a composition that names no catalog reads p.16 unmodified rather than reaching
        /// for <c>MatchContentCatalogs.ForThisBuild</c>, which
        /// <c>EffectSeamConformanceTests</c> forbids a production type doing.</para>
        /// </param>
        public static HeroCurrentCardViewModel CurrentCard(
            IMatchReadModel projection,
            HeroId heroId,
            EffectCapabilityCatalog activeEffectCapabilities = null)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var commitment = projection.Commitments
                .FirstOrDefault(value => value.HeroId.Equals(heroId));
            if (commitment.Status == TurnCardCommitmentStatus.CardCommittedFacedown)
            {
                return new HeroCurrentCardViewModel(commitment.Status);
            }

            if (commitment.Status != TurnCardCommitmentStatus.Revealed ||
                !commitment.RevealedCardInstanceId.HasValue)
            {
                return null;
            }

            var card = projection.Cards
                .FirstOrDefault(value => value.Id.Equals(commitment.RevealedCardInstanceId.Value));
            if (card == null || card.State.ResolutionStatus == CardResolutionStatus.Resolved)
            {
                // The turn is not over - the other hero may still be acting - but this hero's card is,
                // and the numbered slot it belongs to is now occupied. One card, one place.
                return null;
            }

            // The turn ordinal is read the way the tray reads it, including the fallback for a turn-area
            // card this round's history does not name: the group only decides which lifecycle line the
            // face would otherwise carry, and a card in play carries `RESOLVING` in place of all of them.
            var playedTurnIndex = PlayedTurnIndex(projection, heroId, card.Id);

            // Milestone 8.42a read both of these and handed presentation their sum; 8.55 hands over the
            // pair, because the badge now says `6+1` and the sum cannot be taken apart again without
            // presentation re-deriving p.16 for itself. Both readings still come from the authoritative
            // public counts and the one shared item query - the printed number from the definition the
            // instance names, the addend from `ItemBonusQuery`, which owns the matching-icon condition.
            var printedInitiative = PrintedValueQuery.CalculatePrintedInitiative(
                card, projection.Definitions);
            var initiativeItems = projection.HeroProgression
                .FirstOrDefault(value => value.HeroId.Equals(heroId))?
                .ItemBonuses.Where(value => value.Key == PrintedValueChannel.Initiative)
                .Sum(value => value.Value) ?? 0;
            return new HeroCurrentCardViewModel(
                commitment.Status,
                TrayCard(
                    projection,
                    card,
                    playedTurnIndex > 0 ? TrayCardGroup.Played : TrayCardGroup.Discarded,
                    playedTurnIndex,
                    false,
                    false,
                    true),
                printedInitiative.Total,
                // Milestone 8.61h. What one Initiative item adds right now - p.16's own +1, or the
                // double Min's Inner Strength and Perfect Self state for the round. The rail then
                // reads `6+2` with one item under the doubling, which is the same two numbers 8.55
                // split apart rather than a third reading of them.
                ItemBonusQuery.ProjectedItemBonus(
                    printedInitiative,
                    initiativeItems,
                    ActiveEffectVisibility.ItemBonusPerItem(
                        projection, heroId, activeEffectCapabilities)));
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
            IMatchReadModel projection,
            CardInstance card,
            TrayCardGroup group,
            int playedTurnIndex,
            bool isSelectable,
            bool isSelected,
            bool isResolving = false,
            bool isAwaitedAnswer = false,
            bool isDimmedByResolution = false)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (card == null) throw new ArgumentNullException(nameof(card));
            var definition = projection.Definitions
                .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            return new TrayCardViewModel(
                card.Id,
                card.DefinitionReference,
                definition?.DisplayName ?? card.Id.Value,
                definition?.Progression.Color ?? CardColor.None,
                // Milestone 8.57g. A tray card drawn FROM A PROJECTION knows whose card it is and what
                // that hero has unlocked, so it takes p.13's overlaid classification rather than the
                // printed one - see ReadFace's own remarks.
                ReadFace(
                    definition,
                    card.OwnerId,
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        projection.Definitions, projection.Cards)),
                group,
                playedTurnIndex,
                isSelectable,
                isSelected,
                // Milestone 8.5b: asked of the projection, never derived here. Presentation does not
                // know what an Active effect is, only which cards the authority says are sourcing one.
                projection.IsActiveEffectSource(card.Id),
                isResolving,
                isAwaitedAnswer,
                isDimmedByResolution,
                // Milestone 9.32g: a seat that holds the card may read it, facedown or not.
                CardLifecycleAuthority.IsFacedownRestingCard(card),
                SpellbookPresentation.CastableSpells(projection, card.OwnerId, definition));
        }

        /// <summary>
        /// One card still in a hero's deck as a tray card, for a surface that offers it: p.15's Upgrade a
        /// Card draws each candidate replacement as the whole printed face a player will be holding.
        ///
        /// <para>Milestone 8.8a. It goes through the same <see cref="ReadFace"/> every tray card goes
        /// through, so the card a prompt offers and the card that arrives in the hand cannot be drawn as
        /// two different cards. The two states a deck card cannot be in are stated rather than asked:
        /// nothing sourced from it can be a live Active effect, because an effect's source is a card that
        /// was played, and it is not the card being resolved for the same reason.</para>
        /// </summary>
        public static TrayCardViewModel DeckCard(
            CardInstanceId cardInstanceId,
            CardDefinition definition) =>
            new TrayCardViewModel(
                cardInstanceId,
                definition?.Reference ?? default,
                definition?.DisplayName ?? cardInstanceId.Value,
                definition?.Progression.Color ?? CardColor.None,
                ReadFace(definition),
                TrayCardGroup.Deck,
                0,
                true,
                false,
                // Milestone 8.35m: this class's whole point is "a surface that offers it" - the doc
                // comment above already says so - so every candidate it builds is card-scope's leveling
                // upgrade case, unconditionally.
                isAwaitedAnswer: true);

        /// <summary>
        /// Milestone 8.5a: one card definition read into the four value homes and the printed words its
        /// tray card draws, from typed content alone.
        ///
        /// <para>It is definition content formatted, never a modified total: a hero's item bonuses raise
        /// a card's totals, and this face is the card. A definition the projection does not carry - which
        /// only happens for an instance whose content was never registered - reads as a face that states
        /// nothing, rather than faulting a whole tray over one card.</para>
        /// </summary>
        public static TrayCardFaceViewModel ReadFace(CardDefinition definition) =>
            ReadFace(
                definition,
                default,
                GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None);

        /// <summary>
        /// Milestone 8.57g. The same face read for a hero whose unlocked Ultimates are known.
        ///
        /// <para>One card of this build prints an ability that changes what another card's printed
        /// primary IS: Rowenna's Fabled Lance makes every Attack action of hers a p.13 RANGED one while
        /// it sits beside her dashboard. The tray face is where a player reads that word, so the label
        /// asks <c>RangedAttackOverlayQuery</c> - the same query the attack backend and the defence
        /// preview ask - instead of printing <c>ActionDefinition.AttackRange</c> raw.</para>
        ///
        /// <para>The parameterless overload above answers with
        /// <see cref="GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None"/>, which is a
        /// caller holding no card content saying so rather than a default substituted for an absent
        /// argument: p.15's upgrade candidates are DECK cards, and a card in a deck has no unlocked
        /// Ultimate of its own to read.</para>
        /// </summary>
        public static TrayCardFaceViewModel ReadFace(
            CardDefinition definition,
            HeroId ownerId,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
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
                PrimaryActionLabel(definition, primary, ownerId, ultimates),
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
        private static string PrimaryActionLabel(
            CardDefinition definition,
            ActionDefinition primary,
            HeroId ownerId,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates)
        {
            if (primary == null)
            {
                // p.16's Ultimate is the one card with no action at all: it is placed beside the
                // dashboard and its ability is always in effect, so naming its supertype is naming the
                // whole of what it is.
                return definition.Progression.IsUltimate ? "ULTIMATE" : "NO PRIMARY ACTION";
            }

            var kind = primary.Kind.ToString().ToUpperInvariant();
            // Milestone 8.57g. p.13's classification with the Ultimate overlay applied, which is the
            // one typed fact on this face that another card can change. Everything else here is still
            // read straight off the definition, exactly as the paragraph above says.
            if (primary.Kind == ActionKind.Attack &&
                GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery.ClassifyFromUnlockedUltimates(
                    primary.AttackRange, ownerId, ultimates) == AttackRangeKind.Ranged)
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
