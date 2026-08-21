using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The one visual vocabulary every configured match surface draws from: the five card colours, the
    /// six printed-value channels, and the shared control states.
    ///
    /// Milestone 7.8a. It exists so that a colour, a symbol, or a state treatment is decided once rather
    /// than at each of the dozen controls that needs it, and so that the accessibility rule is
    /// structural rather than remembered: <see cref="ColorName"/> exists beside <see cref="Face"/>, and
    /// <see cref="ChannelToken"/> beside <see cref="ChannelName"/>, because nothing in this HUD may mean
    /// something by colour alone.
    ///
    /// It converts nothing. A <see cref="CardColor"/> and a <see cref="TeamSide"/> are separate types
    /// with separate palettes here for the reason <see cref="CardColor"/> itself records: a card's Red
    /// and a team's Red are unrelated, and a surface that reached for one when it meant the other would
    /// be inferring rules identity from a swatch.
    /// </summary>
    public static class MatchHudTheme
    {
        /// <summary>Panel chrome, dark enough that the board reads as the lit surface above it.</summary>
        public static readonly Color PanelBackground = new Color(0.055f, 0.065f, 0.074f, 0.96f);

        public static readonly Color RailBackground = new Color(0.09f, 0.105f, 0.118f, 0.96f);
        public static readonly Color TopBarBackground = new Color(0.031f, 0.039f, 0.043f, 0.98f);
        public static readonly Color PanelBorder = new Color(0.204f, 0.227f, 0.247f, 1f);
        public static readonly Color PrimaryText = new Color(0.933f, 0.945f, 0.953f, 1f);
        public static readonly Color MutedText = new Color(0.663f, 0.690f, 0.710f, 1f);
        public static readonly Color EmptySlot = new Color(0.188f, 0.212f, 0.227f, 1f);

        /// <summary>The pointed-at/keyboard-focused outline, shared by every control family.</summary>
        public static readonly Color FocusOutline = new Color(1f, 1f, 1f, 0.92f);

        /// <summary>What a control the player has selected wears, on cards, modes, and candidates alike.</summary>
        public static readonly Color SelectedOutline = new Color(0.35f, 0.75f, 1f, 1f);

        /// <summary>What a confirmable control wears, distinct from selection because it means "now do it".</summary>
        public static readonly Color ConfirmAccent = new Color(0.95f, 0.62f, 0.25f, 1f);

        /// <summary>
        /// Milestone 8.5b: what a card sourcing a live Active effect wears.
        ///
        /// A third accent rather than a shade of an existing one, because it is orthogonal to both:
        /// selection is what the player has chosen, the confirm accent is what they may do next, and
        /// this is what the rules are still doing on their behalf. A card can be selected and active at
        /// once, so the two treatments have to be told apart at a glance - and neither is the only
        /// thing saying so, since the card also carries the word `ACTIVE`.
        ///
        /// <para>Violet, because the five printed card colours are gold, silver, red, green and blue,
        /// selection is cyan and the confirm accent is orange - so it is the one hue on this HUD that
        /// cannot be mistaken for a card's own colour or for either existing control state.</para>
        /// </summary>
        public static readonly Color ActiveEffectAccent = new Color(0.729f, 0.451f, 0.949f, 1f);

        /// <summary>
        /// Milestone 8.7a: the neutral back a card committed facedown is drawn as.
        ///
        /// It is a colour of its own rather than <see cref="EmptySlot"/> because the two say opposite
        /// things - an empty slot is a turn nobody has played, and this is a card that has been played
        /// and may not be read yet - and rather than any <see cref="Face"/> because the whole point of
        /// a back is that it names no printed colour. Nothing about the card reaches this surface: the
        /// marker that wears it is built from the commitment status alone.
        /// </summary>
        public static readonly Color CardBack = new Color(0.204f, 0.235f, 0.290f, 1f);

        public static readonly Color TeamRed = new Color(0.62f, 0.173f, 0.153f, 1f);
        public static readonly Color TeamBlue = new Color(0.157f, 0.416f, 0.667f, 1f);

        /// <summary>
        /// The five printed colours of rulebook p.7, as surfaces. Each is paired with
        /// <see cref="ColorName"/> wherever it is drawn, so the swatch is a second reading of the word
        /// rather than the only one.
        /// </summary>
        public static Color Face(CardColor color)
        {
            switch (color)
            {
                case CardColor.Gold: return new Color(0.663f, 0.510f, 0.133f, 1f);
                case CardColor.Silver: return new Color(0.608f, 0.643f, 0.667f, 1f);
                case CardColor.Red: return new Color(0.647f, 0.184f, 0.161f, 1f);
                case CardColor.Green: return new Color(0.157f, 0.486f, 0.259f, 1f);
                case CardColor.Blue: return new Color(0.157f, 0.424f, 0.678f, 1f);
                default: return EmptySlot;
            }
        }

        /// <summary>The readable text colour on top of <see cref="Face"/>, chosen for contrast.</summary>
        public static Color FaceText(CardColor color) =>
            color == CardColor.Gold || color == CardColor.Silver
                ? new Color(0.09f, 0.094f, 0.102f, 1f)
                : PrimaryText;

        /// <summary>
        /// A plate drawn on a card's own face - the Initiative flag, the primary-action band, the
        /// secondary-value strip - as a shift of <see cref="Face"/> away from that face's own
        /// <see cref="FaceText"/>.
        ///
        /// Milestone 8.5a. The direction is the point. A plate built by darkening every colour reads
        /// well on Red, Green and Blue and destroys Gold and Silver, whose text is near-black: the
        /// darker the plate, the closer it gets to its own letters. Moving away from the text colour
        /// instead can only raise the plate's contrast against it, whatever the colour and whatever the
        /// strength, which is why the rule is stated as a direction rather than as ten hand-picked
        /// swatches. <c>MatchHudPresentationTests</c> measures the resulting ratio for every printed
        /// colour rather than trusting the construction.
        /// </summary>
        public static Color FacePlate(CardColor color, float strength)
        {
            var face = Face(color);
            var awayFromText = FaceText(color) == PrimaryText ? Color.black : Color.white;
            var plate = Color.Lerp(face, awayFromText, Mathf.Clamp01(strength));
            return new Color(plate.r, plate.g, plate.b, 1f);
        }

        /// <summary>The card's own name band and secondary-value strip: a plate one step off the face.</summary>
        public static Color FaceBand(CardColor color) => FacePlate(color, 0.20f);

        /// <summary>
        /// The Initiative flag and the primary-action band: the strongest plate, because those two are
        /// what a player scans a five-card row for.
        /// </summary>
        public static Color FaceFlag(CardColor color) => FacePlate(color, 0.42f);

        /// <summary>
        /// The card's rules panel, which is a light face on every colour exactly as a printed card is.
        /// The words a card prints are the one thing on it that is never colour-coded.
        /// </summary>
        public static readonly Color RulesPanelBackground = new Color(0.925f, 0.918f, 0.898f, 1f);

        public static readonly Color RulesPanelText = new Color(0.106f, 0.110f, 0.122f, 1f);

        /// <summary>The muted ink the rules panel states an absence in, distinct from printed words.</summary>
        public static readonly Color RulesPanelMutedText = new Color(0.392f, 0.400f, 0.416f, 1f);

        /// <summary>
        /// The card's colour, darkened until it reads as small text on <see cref="RulesPanelBackground"/>.
        /// The p.9 Range/Radius badge wears it, so the badge belongs to its card without becoming the
        /// one thing on the panel that cannot be read.
        /// </summary>
        public static Color RulesPanelAccent(CardColor color)
        {
            var accent = Color.Lerp(Face(color), Color.black, 0.45f);
            return new Color(accent.r, accent.g, accent.b, 1f);
        }

        /// <summary>
        /// The printed colour's own name. A card with no printed colour - milestone 7.3's Ultimate, and
        /// a definition that states no progression at all - says so rather than being given a sixth
        /// colour word it does not have.
        /// </summary>
        public static string ColorName(CardColor color) =>
            color == CardColor.None ? "NO COLOUR" : color.ToString().ToUpperInvariant();

        public static Color TeamFace(TeamSide side) =>
            side == TeamSide.Red ? TeamRed : side == TeamSide.Blue ? TeamBlue : EmptySlot;

        /// <summary>
        /// The compact token a printed-value channel is shown as, for the item-bonus strip and the
        /// card's own value line.
        ///
        /// Text rather than an icon by the milestone 7.8a decision: this project has no rulebook value
        /// icons, and a token that reads as three letters everywhere is honest about that, while a glyph
        /// borrowed from the font would be a different picture on every platform. The full
        /// <see cref="ChannelName"/> stays available for the assistive and hover label.
        /// </summary>
        public static string ChannelToken(PrintedValueChannel channel)
        {
            switch (channel)
            {
                case PrintedValueChannel.Initiative: return "INIT";
                case PrintedValueChannel.Attack: return "ATK";
                case PrintedValueChannel.Defense: return "DEF";
                case PrintedValueChannel.Movement: return "MOV";
                case PrintedValueChannel.Range: return "RNG";
                case PrintedValueChannel.Radius: return "RAD";
                default: return "-";
            }
        }

        public static string ChannelName(PrintedValueChannel channel) =>
            channel == PrintedValueChannel.None ? "no icon" : channel.ToString();

        /// <summary>
        /// The channel order every compact strip uses, so two heroes' bonuses are read the same way and
        /// a strip's order is a fact rather than the order items happened to be gained in.
        /// </summary>
        public static readonly IReadOnlyList<PrintedValueChannel> ChannelOrder = Array.AsReadOnly(
            new[]
            {
                PrintedValueChannel.Initiative,
                PrintedValueChannel.Attack,
                PrintedValueChannel.Defense,
                PrintedValueChannel.Movement,
                PrintedValueChannel.Range,
                PrintedValueChannel.Radius
            });

        /// <summary>
        /// The order this HUD reads a set of differently coloured cards in, when the set itself carries
        /// no order the rules decided: Blue, then Green, then Red, with p.15's two Basic colours after
        /// them.
        ///
        /// <para>Milestone 8.8a, and it is deliberately narrow. Everywhere the authority states an order
        /// - the tray's three lifecycle groups, a pause's own option list, a round's assignment history -
        /// that order is the one drawn, and this is not consulted. It exists for the one surface where a
        /// set of options is laid out as a grid of colour columns rather than as a list: p.15's Upgrade a
        /// Card offers two candidates per upgradable card, the pause orders those pairs by card instance
        /// identity, and a column order taken from that identity would put the same three colours in a
        /// different left-to-right order for every hero. Choosing them nothing decides is not a fact, so
        /// it is stated once here rather than invented at the surface.</para>
        ///
        /// <para>It reorders columns only. Which options exist, which pair each belongs to, and the order
        /// of the candidates inside a pair all stay the projection's, which is what keeps presentation
        /// from inventing choice authority.</para>
        /// </summary>
        public static readonly IReadOnlyList<CardColor> ColorReadingOrder = Array.AsReadOnly(
            new[]
            {
                CardColor.Blue,
                CardColor.Green,
                CardColor.Red,
                CardColor.Gold,
                CardColor.Silver
            });

        /// <summary>
        /// Where <paramref name="color"/> sits in <see cref="ColorReadingOrder"/>. A colour the list does
        /// not name sorts after every colour it does, so an unexpected value is still ordered
        /// deterministically rather than dropped or thrown on.
        /// </summary>
        public static int ColorReadingRank(CardColor color)
        {
            for (var index = 0; index < ColorReadingOrder.Count; index++)
            {
                if (ColorReadingOrder[index] == color)
                {
                    return index;
                }
            }

            return ColorReadingOrder.Count;
        }

        /// <summary>A signed amount as p.16 states one: "+1", never "1".</summary>
        public static string Signed(int amount) =>
            amount >= 0
                ? "+" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
