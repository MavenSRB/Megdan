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

        /// <summary>A signed amount as p.16 states one: "+1", never "1".</summary>
        public static string Signed(int amount) =>
            amount >= 0
                ? "+" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
