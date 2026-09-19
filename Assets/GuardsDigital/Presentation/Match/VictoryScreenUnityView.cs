using System.Collections.Generic;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.53. The victory screen's UI Toolkit surface: a dark scrim over the whole viewport
    /// with one centred panel on it, and nothing else on the screen at all.
    ///
    /// <para><b>Element names, because a journey addresses this surface by them.</b> The root - the
    /// scrim - is <c>gd-victory-screen</c>; the centred column on it is <c>gd-victory-panel</c>. Inside
    /// it: <c>gd-victory-banner</c> (RED WINS), <c>gd-victory-cause</c> (the sentence that ended it),
    /// one <c>gd-victory-block</c> per table with <c>gd-victory-block-title</c> and
    /// <c>gd-victory-block-tag</c> in its header and <c>gd-victory-head-&lt;column&gt;</c> over its
    /// columns, then one <c>gd-victory-row</c> per hero carrying <c>gd-victory-portrait</c>,
    /// <c>gd-victory-name</c>, the rail's own <c>gd-hero-worth</c> badge and one
    /// <c>gd-victory-cell-&lt;column&gt;</c> per number; and <c>gd-victory-legend</c> under them
    /// all.</para>
    ///
    /// <para><b>There are no controls on it.</b> The user asked for a scoreboard, not a menu: there is
    /// no rematch flow in this build and leaving a match belongs to the lobby surface outside the match
    /// HUD, so the mockup's PLAY AGAIN / BACK TO LOBBY pair is deliberately not built. The scrim still
    /// answers the pointer, so a click that used to reach the board lands on the screen instead.</para>
    ///
    /// <para><b>Every size is a reference unit</b> under <see cref="MatchHudPanelScaling"/>, and the
    /// whole panel is a fixed arithmetic fit rather than a measured one - see
    /// <see cref="PanelHeightAt"/> for why that is safe at every supported viewport.</para>
    /// </summary>
    public sealed class VictoryScreenUnityView : IVictoryScreenView
    {
        /// <summary>The panel's inside padding.</summary>
        public const float PanelPadding = 28f;

        /// <summary>One block's inside padding, inside the panel's.</summary>
        public const float BlockPadding = 12f;

        /// <summary>The panel's own border, and one block's - both spent on the table's width.</summary>
        public const float PanelBorderWidth = 2f;

        public const float BlockBorderWidth = 2f;

        /// <summary>The space between two blocks.</summary>
        public const float BlockGap = 14f;

        /// <summary>The hero column's width - a disc, a name and the worth badge beside it.</summary>
        public const float HeroColumnWidth = 416f;

        /// <summary>The hero token disc's own diameter, and the face inside it.</summary>
        public const float PortraitDiscSize = 48f;

        public const float PortraitFaceSize = 42f;

        /// <summary>One hero row's height: the disc plus the breath above and below it.</summary>
        public const float RowHeight = 58f;

        /// <summary>The block's own header band - the team name and its tag.</summary>
        public const float BlockHeaderHeight = 34f;

        /// <summary>The column-header row inside a block.</summary>
        public const float ColumnHeaderHeight = 28f;

        /// <summary>The banner's own line height and font size.</summary>
        public const float BannerHeight = 92f;

        public const float BannerFontSize = 68f;

        /// <summary>The win-condition sentence under the banner.</summary>
        public const float CauseHeight = 34f;

        /// <summary>The legend band under the tables.</summary>
        public const float LegendHeight = 56f;

        /// <summary>The scrim's opacity over the board behind it.</summary>
        public const float ScrimAlpha = 0.92f;

        private static readonly IReadOnlyDictionary<VictoryScreenColumn, float> ColumnWidths =
            new Dictionary<VictoryScreenColumn, float>
            {
                { VictoryScreenColumn.Level, 92f },
                { VictoryScreenColumn.Kills, 92f },
                { VictoryScreenColumn.Deaths, 100f },
                { VictoryScreenColumn.AttacksAtHeroes, 210f },
                { VictoryScreenColumn.TimesDefended, 200f },
                { VictoryScreenColumn.MinionsKilled, 190f },
                { VictoryScreenColumn.CoinsEarned, 180f }
            };

        /// <summary>
        /// Milestone 8.58. The table's own width: the hero column plus every number column, and nothing
        /// else. One set of widths is shared by the header row and by every hero row, which is what
        /// makes this a grid rather than two rows of boxes that happened to be laid out alike.
        /// </summary>
        public static float TableWidth
        {
            get
            {
                var width = HeroColumnWidth;
                foreach (var column in VictoryScreenLabels.Columns)
                {
                    width += ColumnWidths[column];
                }

                return width;
            }
        }

        /// <summary>
        /// The centred panel's own width, DERIVED from the table it has to hold rather than declared
        /// beside it.
        ///
        /// <para><b>Milestone 8.58, and the user's capture that forced it.</b> The panel was a flat
        /// 1560, and the table plus the paddings and borders between it and the panel's edge measured
        /// 1568: eight units of overflow, which clipped the last column - "Coins earned runs past the
        /// panel's right edge" - and, because the hero cell was the one element in the row that could
        /// shrink and the header's could not, pushed every row's numbers out of line with their own
        /// headers. Both are the same arithmetic, so both are fixed by doing it rather than
        /// writing it down.</para>
        /// </summary>
        public static float PanelWidth =>
            TableWidth +
            (BlockPadding * 2f) + (BlockBorderWidth * 2f) +
            (PanelPadding * 2f) + (PanelBorderWidth * 2f);

        private readonly Label banner;
        private readonly Label cause;
        private readonly VisualElement panel;
        private readonly VisualElement blocks;
        private readonly VisualElement legend;

        public VictoryScreenUnityView()
        {
            Root = new VisualElement { name = "gd-victory-screen" };
            Root.style.position = Position.Absolute;
            Root.style.flexDirection = FlexDirection.Column;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.display = DisplayStyle.None;
            // The board reads as background rather than as something still being played on, and the
            // scrim answers the pointer so a click cannot fall through to a piece.
            Root.style.backgroundColor = new Color(
                MatchHudTheme.PanelBackground.r,
                MatchHudTheme.PanelBackground.g,
                MatchHudTheme.PanelBackground.b,
                ScrimAlpha);
            Root.pickingMode = PickingMode.Position;

            panel = HudElements.Panel(MatchHudTheme.RailBackground);
            panel.name = "gd-victory-panel";
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.alignItems = Align.Stretch;
            panel.style.width = PanelWidth;
            panel.style.maxWidth = PanelWidth;
            panel.style.flexShrink = 0f;
            HudElements.Padding(panel, PanelPadding);
            HudElements.Border(
                panel, MatchHudTheme.PanelBorder, PanelBorderWidth, MatchHudTheme.BandCornerRadius);
            Root.Add(panel);

            banner = HudElements.Text(
                string.Empty, BannerFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            banner.name = "gd-victory-banner";
            banner.style.height = BannerHeight;
            banner.style.letterSpacing = 6f;
            banner.style.whiteSpace = WhiteSpace.NoWrap;
            banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(banner);

            cause = HudElements.Text(string.Empty, 18f, MatchHudTheme.MutedText, FontStyle.Bold);
            cause.name = "gd-victory-cause";
            cause.style.height = CauseHeight;
            cause.style.letterSpacing = 2.4f;
            cause.style.whiteSpace = WhiteSpace.NoWrap;
            cause.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(cause);

            blocks = new VisualElement { name = "gd-victory-blocks" };
            blocks.style.flexDirection = FlexDirection.Column;
            blocks.style.marginTop = 10f;
            panel.Add(blocks);

            legend = new VisualElement { name = "gd-victory-legend" };
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.flexWrap = Wrap.Wrap;
            legend.style.marginTop = 12f;
            panel.Add(legend);
        }

        /// <summary>The scrim, which the renderer sizes to the whole viewport.</summary>
        public VisualElement Root { get; }

        /// <summary>The screen currently drawn, or null while the match is still being played.</summary>
        public VictoryScreenViewModel Model { get; private set; }

        /// <summary>Whether a screen is drawn at all, for a driver with no pointer.</summary>
        public bool HasScreen => Model != null;

        /// <summary>
        /// The centred panel's own height for a model, in reference units - what the screen NEEDS.
        ///
        /// <para>It is arithmetic rather than a measurement because the panel this HUD draws on is
        /// never smaller than 2560x1440 in reference units - <c>MatchHudPanelScaling</c>'s
        /// <c>Expand</c> match mode resolves the scale from whichever axis demands more - so a fit
        /// proved at the reference is a fit at 1280x720 and at every other supported viewport. The
        /// worst case this build can compose is rulebook p.6's three a side split three ways by a solo
        /// ending: three blocks, six rows, which this method's own numbers put at well under half the
        /// reference height. The geometry gate measures it rather than trusting the sum.</para>
        /// </summary>
        public static float PanelHeightAt(VictoryScreenViewModel model)
        {
            var height = (PanelPadding * 2f) + BannerHeight + CauseHeight + 10f + LegendHeight + 12f;
            if (model == null)
            {
                return height;
            }

            for (var index = 0; index < model.Blocks.Count; index++)
            {
                height += BlockHeaderHeight + ColumnHeaderHeight + (BlockPadding * 2f) +
                    (model.Blocks[index].Rows.Count * RowHeight);
                if (index > 0)
                {
                    height += BlockGap;
                }
            }

            return height;
        }

        public void Render(VictoryScreenViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            Rebuild();
        }

        private void Rebuild()
        {
            blocks.Clear();
            legend.Clear();
            if (Model == null)
            {
                Root.style.display = DisplayStyle.None;
                return;
            }

            Root.style.display = DisplayStyle.Flex;
            banner.text = Model.BannerText;
            banner.style.color = Model.BannerColor;
            cause.text = Model.CauseText;

            foreach (var block in Model.Blocks)
            {
                blocks.Add(BuildBlock(block));
            }

            foreach (var entry in Model.Legend)
            {
                legend.Add(BuildLegendEntry(entry));
            }
        }

        private static VisualElement BuildLegendEntry(VictoryScreenLegendEntry entry)
        {
            var row = new VisualElement { name = "gd-victory-legend-entry" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginRight = 22f;
            row.style.marginTop = 2f;
            var term = HudElements.Text(entry.Term, 11f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            term.style.marginRight = 5f;
            row.Add(term);
            row.Add(HudElements.Text(entry.Description, 11f, MatchHudTheme.MutedText));
            return row;
        }

        private static VisualElement BuildBlock(VictoryScreenBlockViewModel model)
        {
            var host = HudElements.Panel(MatchHudTheme.PanelBackground);
            host.name = "gd-victory-block";
            host.style.flexDirection = FlexDirection.Column;
            host.style.marginTop = BlockGap;
            HudElements.Padding(host, BlockPadding);
            // The winners' table - and the solo winner's - is rimmed in the coin's gold; everybody
            // else keeps the HUD's ordinary panel border.
            var isWinner = model.Kind != VictoryScreenBlockKind.Defeated;
            // Milestone 8.58: the SAME border width either way, because the block's border is spent on
            // the width the table has to fit inside and a winner's table cannot be two units narrower
            // than a loser's or the two grids would not line up down the screen.
            HudElements.Border(
                host,
                isWinner ? MatchHudTheme.CoinFace : MatchHudTheme.PanelBorder,
                BlockBorderWidth,
                10f);

            var header = new VisualElement { name = "gd-victory-block-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.height = BlockHeaderHeight;
            host.Add(header);

            var accent = TeamAccent(model.TeamSide);
            var title = HudElements.Text(model.Header, 16f, accent, FontStyle.Bold);
            title.name = "gd-victory-block-title";
            title.style.letterSpacing = 2f;
            title.style.whiteSpace = WhiteSpace.NoWrap;
            header.Add(title);

            var tag = HudElements.Text(
                model.Tag,
                12f,
                isWinner ? MatchHudTheme.CoinInk : MatchHudTheme.MutedText,
                FontStyle.Bold);
            tag.name = "gd-victory-block-tag";
            tag.style.letterSpacing = 1.6f;
            tag.style.whiteSpace = WhiteSpace.NoWrap;
            tag.style.paddingLeft = 10f;
            tag.style.paddingRight = 10f;
            tag.style.paddingTop = 3f;
            tag.style.paddingBottom = 3f;
            tag.style.backgroundColor = isWinner ? MatchHudTheme.CoinFace : MatchHudTheme.EmptySlot;
            HudElements.Border(
                tag,
                isWinner ? MatchHudTheme.CoinEdge : MatchHudTheme.PanelBorder,
                1f,
                9f);
            header.Add(tag);

            host.Add(BuildColumnHeaders());
            foreach (var row in model.Rows)
            {
                host.Add(BuildRow(row));
            }

            return host;
        }

        /// <summary>
        /// Milestone 8.58. One column's geometry, applied identically to the header cell and to every
        /// hero row's cell in that column - which is the whole of what makes the table a grid.
        ///
        /// <para>The hero column is the only flexible one, and it is flexible in BOTH rows with the same
        /// basis and the same weight, so a narrow panel shortens the name and never moves a number out
        /// from under its own header. Every number column is rigid in both.</para>
        /// </summary>
        private static void ApplyColumnGeometry(VisualElement cell, float width, bool isHeroColumn)
        {
            cell.style.width = width;
            cell.style.minWidth = isHeroColumn ? 0f : width;
            cell.style.maxWidth = width;
            cell.style.flexBasis = width;
            cell.style.flexGrow = 0f;
            cell.style.flexShrink = isHeroColumn ? 1f : 0f;
        }

        private static VisualElement BuildColumnHeaders()
        {
            var row = new VisualElement { name = "gd-victory-head" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = ColumnHeaderHeight;
            row.style.flexShrink = 0f;

            var hero = HudElements.Text("Hero", 11f, MatchHudTheme.MutedText, FontStyle.Bold);
            hero.name = "gd-victory-head-hero";
            ApplyColumnGeometry(hero, HeroColumnWidth, isHeroColumn: true);
            hero.style.letterSpacing = 1.4f;
            hero.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(hero);

            foreach (var column in VictoryScreenLabels.Columns)
            {
                var cell = HudElements.Text(
                    VictoryScreenLabels.Header(column), 11f, MatchHudTheme.MutedText, FontStyle.Bold);
                cell.name = VictoryScreenLabels.HeaderCellName(column);
                ApplyColumnGeometry(cell, ColumnWidths[column], isHeroColumn: false);
                cell.style.letterSpacing = 1.4f;
                cell.style.whiteSpace = WhiteSpace.NoWrap;
                cell.style.unityTextAlign = TextAnchor.MiddleRight;
                cell.style.overflow = Overflow.Hidden;
                row.Add(cell);
            }

            return row;
        }

        private static VisualElement BuildRow(VictoryScreenRowViewModel model)
        {
            var row = new VisualElement { name = "gd-victory-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = RowHeight;
            row.style.flexShrink = 0f;
            row.style.borderTopWidth = 1f;
            row.style.borderTopColor = MatchHudTheme.PanelBorder;

            var identity = new VisualElement { name = "gd-victory-hero" };
            identity.style.flexDirection = FlexDirection.Row;
            identity.style.alignItems = Align.Center;
            ApplyColumnGeometry(identity, HeroColumnWidth, isHeroColumn: true);
            identity.style.overflow = Overflow.Hidden;
            identity.Add(BuildPortrait(model));

            var name = HudElements.Text(
                model.DisplayName, 17f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            name.name = "gd-victory-name";
            name.style.marginLeft = 10f;
            name.style.flexShrink = 1f;
            name.style.whiteSpace = WhiteSpace.NoWrap;
            identity.Add(name);
            if (model.Worth > 0)
            {
                // Milestone 8.58: the rail's own badge at the rail's own size - one badge, not two that
                // have to be kept looking alike.
                var badge = HudElements.WorthBadge(model.WorthLabel, model.WorthAssistiveLabel);
                badge.style.marginLeft = 8f;
                identity.Add(badge);
            }

            row.Add(identity);

            foreach (var cell in model.Cells)
            {
                var label = HudElements.Text(
                    cell.Text,
                    20f,
                    cell.IsBest ? MatchHudTheme.ConfirmAccent : MatchHudTheme.PrimaryText,
                    cell.IsBest ? FontStyle.Bold : FontStyle.Normal);
                label.name = cell.ElementName;
                ApplyColumnGeometry(label, ColumnWidths[cell.Column], isHeroColumn: false);
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.unityTextAlign = TextAnchor.MiddleRight;
                label.style.overflow = Overflow.Hidden;
                label.tooltip = cell.Header + ": " + cell.Text;
                row.Add(label);
            }

            return row;
        }

        /// <summary>
        /// The hero's own token face in a disc rimmed in their team's accent - addressed exactly as the
        /// hero rail's medallion and the piece on the board are, by projected content identity and
        /// authoritative team. The private artwork is git-ignored and a clean checkout resolves none,
        /// in which case the disc is the team accent with nothing in it and the name beside it carries
        /// the identity, which is the rail's own behaviour rather than a second rule.
        /// </summary>
        private static VisualElement BuildPortrait(VictoryScreenRowViewModel model)
        {
            var accent = TeamAccent(model.TeamSide);
            var medallion = new VisualElement { name = "gd-victory-portrait" };
            medallion.style.width = PortraitDiscSize;
            medallion.style.height = PortraitDiscSize;
            medallion.style.flexShrink = 0f;
            medallion.style.alignItems = Align.Center;
            medallion.style.justifyContent = Justify.Center;
            medallion.style.backgroundColor = Color.Lerp(MatchHudTheme.PanelBackground, accent, 0.30f);
            medallion.tooltip = model.PortraitAssistiveLabel;
            HudElements.Border(medallion, accent, 2f, PortraitDiscSize / 2f);

            var icon = new VisualElement { name = "gd-victory-portrait-face" };
            icon.style.width = PortraitFaceSize;
            icon.style.height = PortraitFaceSize;
            icon.style.flexShrink = 0f;
            icon.style.backgroundColor = accent;
            icon.style.overflow = Overflow.Hidden;
            icon.tooltip = model.PortraitAssistiveLabel;
            HudElements.Border(
                icon,
                new Color(
                    MatchHudTheme.PrimaryText.r,
                    MatchHudTheme.PrimaryText.g,
                    MatchHudTheme.PrimaryText.b,
                    0.72f),
                1f,
                PortraitFaceSize / 2f);

            if (model.ContentId.IsDefined)
            {
                var token = HeroTokenArtwork.ResolveToken(model.ContentId, model.TeamSide, out var portrait);
                if (portrait != null)
                {
                    var faceSize = PortraitFaceSize * token.FaceRadius /
                        RuntimeTeamIdentity.TeamBakedFaceRadius;
                    icon.style.width = faceSize;
                    icon.style.height = faceSize;
                    icon.style.backgroundImage = new StyleBackground(portrait);
                    icon.style.backgroundSize =
                        new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                    icon.style.backgroundPositionX = new StyleBackgroundPosition(
                        new BackgroundPosition(BackgroundPositionKeyword.Center));
                    icon.style.backgroundPositionY = new StyleBackgroundPosition(
                        new BackgroundPosition(BackgroundPositionKeyword.Center));
                }
            }

            medallion.Add(icon);
            return medallion;
        }

        private static Color TeamAccent(TeamSide side) =>
            side == TeamSide.Red ? MatchHudTheme.TeamRed : MatchHudTheme.TeamBlue;
    }
}
