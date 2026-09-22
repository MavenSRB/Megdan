using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.35a. The primitive private lobby drawn in UI Toolkit: a seat table, one line of
    /// status, one line of feedback, and exactly the controls the model offers.
    /// </summary>
    /// <remarks>
    /// <para>Primitive on purpose - the block's non-goals rule out production styling - but not
    /// careless about the two things its third acceptance check names. Team, occupancy and
    /// readiness are drawn as words from <see cref="PrivateLobbyLabels"/>, so nothing here is
    /// legible only in colour; and every size is written in the panel's own reference units under
    /// <see cref="MatchHudPanelScaling"/>, with the frame itself bounded as a share of the
    /// viewport, so the same controls are reachable at every supported desktop resolution rather
    /// than at the one they were authored at.</para>
    /// <para>The seat rows and the controls are rebuilt on every render rather than diffed. That is
    /// the primitive choice, and it is also the one that cannot leave a control standing after the
    /// state that offered it has gone - which is the property milestone 8.34 made terminal and
    /// milestone 8.35b's handoff depends on.</para>
    /// </remarks>
    public sealed class PrivateLobbyUnityView : IPrivateLobbyView
    {
        public const string RootName = "gd-lobby-root";
        public const string PanelName = "gd-lobby-panel";
        public const string StatusName = "gd-lobby-status";
        public const string SessionName = "gd-lobby-session";
        public const string FeedbackName = "gd-lobby-feedback";
        public const string EntryName = "gd-lobby-entry";
        public const string AddressFieldName = "gd-lobby-address";
        public const string PortFieldName = "gd-lobby-port";
        public const string KeyFieldName = "gd-lobby-key";
        public const string PickedSeatName = "gd-lobby-picked-seat";
        public const string SeatsName = "gd-lobby-seats";
        public const string SeatRowName = "gd-lobby-seat-row";
        public const string SeatPickName = "gd-lobby-seat-pick";
        public const string ControlsName = "gd-lobby-controls";
        public const string ControlName = "gd-lobby-control";
        public const string BuildStampName = "gd-lobby-build-stamp";

        /// <summary>Milestone 8.35z37. Rulebook p.22's selection screen.</summary>
        public const string SelectionStatusName = "gd-lobby-selection-status";
        public const string HeroesName = "gd-lobby-heroes";
        public const string HeroOptionName = "gd-lobby-hero-option";
        public const string HeroFiltersName = "gd-lobby-hero-filters";
        public const string HeroStarFilterName = "gd-lobby-hero-star-filter";
        public const string HeroRoleFilterName = "gd-lobby-hero-role-filter";
        public const string HeroFilterClearName = "gd-lobby-hero-filter-clear";
        public const string HeroFilterEmptyName = "gd-lobby-hero-filter-empty";
        public const string HeroOverviewName = "gd-lobby-hero-overview";
        public const string WordmarkName = "gd-lobby-wordmark";

        /// <summary>The default invitation: this machine, on the protocol's own port.</summary>
        public const string DefaultAddress = "127.0.0.1";

        private static readonly Color PanelBackground = new Color(0.06f, 0.07f, 0.09f, 0.97f);
        private static readonly Color PanelText = new Color(0.93f, 0.94f, 0.96f, 1f);

        /// <summary>
        /// Milestone 8.35z58: this used to be its own literal, numerically identical to
        /// the theme's button border by coincidence rather than by source. It now reads
        /// <see cref="MatchHudTheme.ButtonBorder"/> directly, so the lobby panel's own frame and
        /// every push-button's border are provably the one colour rather than two copies that
        /// happen to agree today.
        /// </summary>
        private static readonly Color PanelEdge = MatchHudTheme.ButtonBorder;

        private readonly VisualElement seats;
        private readonly VisualElement controls;
        private readonly VisualElement entry;
        private readonly Label status;
        private readonly Label session;
        private readonly Label feedback;
        private readonly Label pickedSeat;
        private readonly TextField address;
        private readonly TextField port;
        private readonly TextField key;
        private readonly Label selectionStatus;
        private readonly VisualElement heroFilters;
        private readonly VisualElement starFilters;
        private readonly VisualElement roleFilters;
        private readonly VisualElement heroes;
        private readonly Label emptyHeroFilters;
        private readonly Image heroOverview;
        private readonly Label wordmark;
        private SeatId picked;

        /// <summary>
        /// Milestone 8.35z37. The hero the next PICK HERO press sends, chosen by clicking
        /// one of the options this view drew. It is a view-local highlight and nothing more: whether
        /// the seat then HOLDS that hero is the roster's answer.
        /// </summary>
        private string highlightedHero = string.Empty;
        private HeroComplexity? selectedComplexity;
        private readonly HashSet<HeroRole> selectedRoles = new HashSet<HeroRole>();
        private PrivateLobbySelection renderedSelection = PrivateLobbySelection.NotRunning;

        private static readonly HeroRole[] PrintedRoles =
        {
            HeroRole.Damager,
            HeroRole.Disabler,
            HeroRole.Durable,
            HeroRole.Farming,
            HeroRole.Healer,
            HeroRole.Melee,
            HeroRole.Pusher,
            HeroRole.Sniper,
            HeroRole.Tactician,
            HeroRole.Tokens
        };

        public PrivateLobbyUnityView()
        {
            Root = new VisualElement { name = RootName };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.right = 0f;
            Root.style.bottom = 0f;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;

            wordmark = Caption("GUARDS OF ATLANTIS II", 40, WordmarkName);
            wordmark.style.color = MatchHudTheme.CoinFace;
            wordmark.style.marginBottom = 16f;
            wordmark.pickingMode = PickingMode.Ignore;
            Root.Add(wordmark);

            var frame = new ScrollView(ScrollViewMode.Vertical) { name = PanelName };
            frame.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            frame.verticalScrollerVisibility = ScrollerVisibility.Auto;
            frame.style.width = new StyleLength(new Length(72f, LengthUnit.Percent));
            frame.style.maxWidth = 1800f;
            frame.style.maxHeight = new StyleLength(new Length(82f, LengthUnit.Percent));
            frame.style.flexShrink = 1f;
            frame.style.paddingLeft = 24f;
            frame.style.paddingRight = 24f;
            frame.style.paddingTop = 20f;
            frame.style.paddingBottom = 20f;
            frame.style.backgroundColor = PanelBackground;
            frame.style.borderTopWidth = 2f;
            frame.style.borderBottomWidth = 2f;
            frame.style.borderLeftWidth = 2f;
            frame.style.borderRightWidth = 2f;
            frame.style.borderTopColor = PanelEdge;
            frame.style.borderBottomColor = PanelEdge;
            frame.style.borderLeftColor = PanelEdge;
            frame.style.borderRightColor = PanelEdge;
            frame.style.flexDirection = FlexDirection.Column;
            Root.Add(frame);

            frame.Add(Caption("PRIVATE LOBBY", 30, "gd-lobby-title"));
            status = Caption(string.Empty, 22, StatusName);
            frame.Add(status);
            session = Caption(string.Empty, 16, SessionName);
            frame.Add(session);
            feedback = Caption(string.Empty, 18, FeedbackName);
            feedback.style.whiteSpace = WhiteSpace.Normal;
            frame.Add(feedback);

            entry = new VisualElement { name = EntryName };
            entry.style.flexDirection = FlexDirection.Column;
            entry.style.marginTop = 8f;
            address = Field("ADDRESS", DefaultAddress, AddressFieldName);
            port = Field(
                "PORT",
                PrivateSessionProtocol.DefaultPort.ToString(CultureInfo.InvariantCulture),
                PortFieldName);
            key = Field("SESSION KEY", string.Empty, KeyFieldName);
            entry.Add(address);
            entry.Add(port);
            entry.Add(key);
            pickedSeat = Caption("SEAT: NONE PICKED", 18, PickedSeatName);
            entry.Add(pickedSeat);
            frame.Add(entry);

            seats = new VisualElement { name = SeatsName };
            seats.style.flexDirection = FlexDirection.Column;
            seats.style.marginTop = 10f;
            frame.Add(seats);

            // Milestone 8.35z37. Rulebook p.22's selection screen, between the seat table
            // and the controls: one line saying whose pick it is, then every registered hero, the
            // taken ones greyed and NAMED with the one-copy rule's reason.
            selectionStatus = Caption(string.Empty, 18, SelectionStatusName);
            selectionStatus.style.marginTop = 10f;
            frame.Add(selectionStatus);

            // Milestone 8.35z86. Two deliberately unlabeled rows: the four star readings are
            // self-describing, and the ten printed role names need no second heading above them.
            // Filter state belongs to this one player's view and never enters a command or projection.
            heroFilters = new VisualElement { name = HeroFiltersName };
            heroFilters.style.flexDirection = FlexDirection.Column;
            frame.Add(heroFilters);

            starFilters = new VisualElement();
            starFilters.style.flexDirection = FlexDirection.Row;
            starFilters.style.flexWrap = Wrap.Wrap;
            foreach (HeroComplexity complexity in Enum.GetValues(typeof(HeroComplexity)))
            {
                var captured = complexity;
                starFilters.Add(BuildHeroFilterButton(
                    new string('★', (int)complexity), HeroStarFilterName,
                    () =>
                    {
                        selectedComplexity = selectedComplexity == captured
                            ? (HeroComplexity?)null
                            : captured;
                        RenderSelection(renderedSelection);
                    },
                    complexity));
            }
            starFilters.Add(BuildHeroFilterButton(
                "CLEAR", HeroFilterClearName,
                () =>
                {
                    selectedComplexity = null;
                    selectedRoles.Clear();
                    RenderSelection(renderedSelection);
                },
                null));
            heroFilters.Add(starFilters);

            roleFilters = new VisualElement();
            roleFilters.style.flexDirection = FlexDirection.Row;
            roleFilters.style.flexWrap = Wrap.Wrap;
            foreach (var role in PrintedRoles)
            {
                var captured = role;
                roleFilters.Add(BuildHeroFilterButton(
                    role.ToString().ToUpperInvariant(), HeroRoleFilterName,
                    () =>
                    {
                        if (!selectedRoles.Add(captured)) selectedRoles.Remove(captured);
                        RenderSelection(renderedSelection);
                    },
                    role));
            }
            heroFilters.Add(roleFilters);

            heroes = new VisualElement { name = HeroesName };
            heroes.style.flexDirection = FlexDirection.Row;
            heroes.style.flexWrap = Wrap.Wrap;
            frame.Add(heroes);

            emptyHeroFilters = Caption(
                "NO HEROES MATCH THESE FILTERS", 16, HeroFilterEmptyName);
            emptyHeroFilters.style.display = DisplayStyle.None;
            frame.Add(emptyHeroFilters);

            controls = new VisualElement { name = ControlsName };
            controls.style.flexDirection = FlexDirection.Row;
            controls.style.flexWrap = Wrap.Wrap;
            controls.style.marginTop = 12f;
            frame.Add(controls);

            // Coordinator-directed small QoL item (2026-09-05, PLAN.md's "SMALL QoL ITEM"): the
            // build/package stamp visible at a glance, so "which build am I running" never again
            // costs the user an hour to a stale package folder. A quiet corner watermark on the
            // whole lobby screen rather than inside the centred panel, so it reads before a session
            // even exists and never competes with the panel's own status/session lines.
            var buildStamp = Caption(BuildIdentityStamp.Current, 12, BuildStampName);
            buildStamp.style.position = Position.Absolute;
            buildStamp.style.left = 10f;
            buildStamp.style.bottom = 8f;
            buildStamp.style.color = new Color(PanelText.r, PanelText.g, PanelText.b, 0.5f);
            buildStamp.pickingMode = PickingMode.Ignore;
            Root.Add(buildStamp);
            heroOverview = new Image { name = HeroOverviewName, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            heroOverview.style.position = Position.Absolute;
            heroOverview.style.display = DisplayStyle.None;
            Root.Add(heroOverview);
            Root.RegisterCallback<GeometryChangedEvent>(_ => HideHeroOverview());
        }

        public VisualElement Root { get; }

        /// <summary>Raised when a player presses one of the controls this view drew.</summary>
        public event Action<PrivateLobbyControl, PrivateLobbyEntry> ControlPressed;

        /// <summary>
        /// Milestone 8.35z37. Raised when a player clicked a hero option. It says which hero
        /// the next PICK HERO press would send and nothing else - clicking a hero does not pick it,
        /// exactly as clicking a seat row does not join it.
        /// </summary>
        public event Action<string> HeroHighlighted;

        /// <summary>The hero the next PICK HERO press would send, for a journey that reads it.</summary>
        public string HighlightedHero => highlightedHero;

        public HeroComplexity? SelectedComplexityFilter => selectedComplexity;
        public IReadOnlyCollection<HeroRole> SelectedRoleFilters => selectedRoles;

        /// <summary>The invitation currently typed into the entry form, plus the picked seat.</summary>
        public PrivateLobbyEntry CurrentEntry() =>
            new PrivateLobbyEntry(address.value, port.value, key.value, picked);

        /// <summary>Picks the seat a join will claim, exactly as pressing its row does.</summary>
        public void PickSeat(SeatId seat)
        {
            picked = seat;
            pickedSeat.text = seat.IsDefined
                ? "SEAT: " + PrivateLobbyLabels.Identifier(seat.Value)
                : "SEAT: NONE PICKED";
        }

        public void Render(PrivateLobbyModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Root.style.display = model.IsShown ? DisplayStyle.Flex : DisplayStyle.None;
            Root.pickingMode = model.IsShown ? PickingMode.Position : PickingMode.Ignore;
            Root.style.backgroundColor = model.HasMatchStarted ? Color.clear : new Color(0.025f, 0.04f, 0.05f, 1f);
            wordmark.style.display = model.HasMatchStarted ? DisplayStyle.None : DisplayStyle.Flex;
            status.text = model.StatusLabel;
            session.text = model.SessionLabel.Length == 0
                ? string.Empty
                : "SESSION " + model.SessionLabel;
            feedback.text = model.FeedbackLabel;
            feedback.style.display = model.FeedbackLabel.Length == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            var isEntry = model.Role == PrivateLobbyRole.None;
            entry.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;

            // 8.35z54. THE PRE-JOIN SEAT PICKER IS GONE. "We lose the whole seat/hero select
            // thing, we connect to the host, see available seats and pick one": the entry form's
            // seat table is now a plain listing of the seats this format fields, and the button that
            // TAKES a seat appears only once this player is in the session and the roster it is
            // reading is the host's rather than a local guess at one.
            var offerClaim = model.Offers(PrivateLobbyControl.ClaimSeat);
            seats.Clear();
            foreach (var row in model.Seats)
            {
                seats.Add(BuildSeatRow(
                    row, offerClaim && row.OccupancyLabel == PrivateLobbyLabels.Occupancy(
                        SeatOccupancyState.Open)));
            }

            RenderSelection(model.Selection);

            controls.Clear();
            foreach (var control in model.Controls)
            {
                controls.Add(BuildControl(control));
            }
        }

        /// <summary>
        /// Milestone 8.35z37. Draws rulebook p.22's All Pick: whose turn it is, and every
        /// registered hero with the taken ones greyed and told why.
        /// </summary>
        /// <remarks>
        /// The whole panel disappears when no selection is running, so a session that predates this
        /// milestone draws exactly the lobby it always did. A taken hero stays on screen rather than
        /// being removed, because a player who watched a hero vanish learns nothing; greyed with the
        /// one-copy rule's own sentence, they learn what happened.
        /// </remarks>
        private void RenderSelection(PrivateLobbySelection selection)
        {
            renderedSelection = selection ?? PrivateLobbySelection.NotRunning;
            HideHeroOverview();
            var running = selection != null && selection.IsRunning;
            selectionStatus.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
            heroFilters.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
            heroes.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
            emptyHeroFilters.style.display = DisplayStyle.None;
            heroes.Clear();
            if (!running)
            {
                highlightedHero = string.Empty;
                HeroHighlighted?.Invoke(string.Empty);
                selectionStatus.text = string.Empty;
                return;
            }

            var displayed = selection.Heroes.Where(MatchesHeroFilters).ToArray();
            if (!selection.IsOwnPick || !displayed.Any(option =>
                    option.IsAvailable && option.HeroIdValue == highlightedHero))
            {
                highlightedHero = string.Empty;
                HeroHighlighted?.Invoke(string.Empty);
            }
            selectionStatus.text = selection.StatusLabel;
            // Milestone 8.76, the user: "make the window fixed size so it fits all the heroes, and
            // dont resize it when selecting filters. just show/hide heroes." EVERY hero keeps its
            // tile in the flow, so the panel is always the size of the whole roster and no button
            // moves when a filter changes; a hero the filters exclude is hidden in place (drawn
            // nothing, picked by nothing, focused by nothing) rather than removed from the tree.
            foreach (var option in selection.Heroes)
            {
                var tile = BuildHeroOption(option, selection.IsOwnPick);
                SetHeroTileShown(tile, MatchesHeroFilters(option));
                heroes.Add(tile);
            }
            emptyHeroFilters.style.display = displayed.Length == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            RefreshHeroFilterHighlights();
            RefreshHeroHighlights();
        }

        /// <summary>
        /// Milestone 8.76. A filtered-out hero keeps its slot and loses everything else: it is not
        /// drawn, its tile and button take no pointer (so the hover overview cannot open on an empty
        /// slot) and its button takes no focus.
        /// </summary>
        private static void SetHeroTileShown(VisualElement tile, bool shown)
        {
            tile.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            tile.pickingMode = shown ? PickingMode.Position : PickingMode.Ignore;
            foreach (var button in tile.Query<Button>(HeroOptionName).ToList())
            {
                button.pickingMode = shown ? PickingMode.Position : PickingMode.Ignore;
                button.focusable = shown;
            }
        }

        private bool MatchesHeroFilters(PrivateLobbyHeroOption option)
        {
            if (selectedComplexity.HasValue && option.Complexity != selectedComplexity.Value)
            {
                return false;
            }

            var required = HeroRole.None;
            foreach (var role in selectedRoles) required |= role;
            return (option.Roles & required) == required;
        }

        private Button BuildHeroFilterButton(
            string text, string name, Action pressed, object value)
        {
            var button = new Button(pressed)
            {
                name = name,
                text = text,
                userData = value
            };
            button.style.marginRight = 6f;
            button.style.marginTop = 6f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.minHeight = 34f;
            ApplyButtonChrome(button);
            return button;
        }

        private void RefreshHeroFilterHighlights()
        {
            foreach (var button in starFilters.Query<Button>(HeroStarFilterName).ToList())
            {
                SetFilterHighlight(
                    button,
                    selectedComplexity.HasValue &&
                    button.userData is HeroComplexity complexity &&
                    complexity == selectedComplexity.Value);
            }

            foreach (var button in roleFilters.Query<Button>(HeroRoleFilterName).ToList())
            {
                SetFilterHighlight(
                    button,
                    button.userData is HeroRole role && selectedRoles.Contains(role));
            }
        }

        private static void SetFilterHighlight(VisualElement button, bool selected)
        {
            var edge = selected ? MatchHudTheme.CoinFace : PanelEdge;
            // Milestone 8.76: the selected filter is told by its colour alone. A thicker edge grew
            // the filter row by a pixel and pushed every hero below it down, which is exactly the
            // resize the user asked to be rid of ("dont resize it when selecting filters").
            var width = 1f;
            HudElements.Border(button, edge, width, MatchHudTheme.ButtonCornerRadius);
        }

        private VisualElement BuildHeroOption(PrivateLobbyHeroOption option, bool isOwnPick)
        {
            var pickable = option.IsAvailable && isOwnPick;
            var button = new Button(() =>
            {
                if (!pickable)
                {
                    return;
                }

                highlightedHero = option.HeroIdValue;
                RefreshHeroHighlights();
                HeroHighlighted?.Invoke(option.HeroIdValue);
            })
            {
                name = HeroOptionName,
                userData = option,
                // Milestone 8.76, the user on the pick screen: "when hero is taken by another player,
                // button is grayed out. The text 'another player...' is unessasary" and "I dont need
                // a reason as a tooltip at all, it is obvious. Mouse over is still a pick card." The
                // greyed chrome is the whole statement, and the hover keeps opening the overview
                // card exactly as it does on any other hero - the SIXTH amendment's hover reason is
                // the user's own call to drop here, on a control whose meaning is self-evident.
                text = option.DisplayName
            };
            button.style.marginRight = 6f;
            button.style.marginTop = 6f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.minHeight = 34f;
            // Milestone 8.35z58. This control was added in 8.35z37, after the
            // EIGHTEENTH amendment's button-system pass (8.35z8) already shipped everywhere else
            // in this file - so it never picked up the shared chrome/motion and drew the same
            // stock-skin-with-a-hand-rolled-border look 8.35z8 replaced for HOST/JOIN/FILL/LEAVE
            // and the seat PICK. ApplyButtonChrome gives it the shared idle background, corner
            // radius and press/hover motion; every line below still wins because it runs after -
            // the dim for an unavailable hero and the highlight ring for the current pick are
            // this control's own established meaning, not part of the four idle/hover/pressed/
            // disabled states, and neither is touched here.
            ApplyButtonChrome(button);
            button.SetEnabled(pickable);
            button.style.color = option.IsAvailable
                ? PanelText
                : new Color(PanelText.r, PanelText.g, PanelText.b, 0.45f);
            button.style.borderTopWidth = 2f;
            button.style.borderBottomWidth = 2f;
            button.style.borderLeftWidth = 2f;
            button.style.borderRightWidth = 2f;
            var edge = string.Equals(
                    highlightedHero, option.HeroIdValue, StringComparison.Ordinal)
                ? PanelText
                : PanelEdge;
            button.style.borderTopColor = edge;
            button.style.borderBottomColor = edge;
            button.style.borderLeftColor = edge;
            button.style.borderRightColor = edge;
            var tile = new VisualElement();
            tile.Add(button);
            // The wrapper still receives hover when the contained choice is unavailable.
            tile.RegisterCallback<PointerEnterEvent>(_ => ShowHeroOverview(option.HeroIdValue, button.worldBound.max));
            tile.RegisterCallback<PointerLeaveEvent>(_ => HideHeroOverview());
            button.RegisterCallback<FocusInEvent>(_ => ShowHeroOverview(option.HeroIdValue, button.worldBound.max));
            button.RegisterCallback<FocusOutEvent>(_ => HideHeroOverview());
            return tile;
        }

        private void RefreshHeroHighlights()
        {
            foreach (var button in heroes.Query<Button>(HeroOptionName).ToList())
            {
                var option = (PrivateLobbyHeroOption)button.userData;
                var selected = highlightedHero == option.HeroIdValue;
                var color = selected ? new Color(1f, 0.82f, 0.35f, 1f) : PanelEdge;
                button.style.borderTopColor = button.style.borderBottomColor = color;
                button.style.borderLeftColor = button.style.borderRightColor = color;
                // Milestone 8.76: the gold ring and the class say "selected"; the user: "I already
                // see the gold outline, i dont need the text." The label is the name alone.
                button.EnableInClassList("gd-hero-selected", selected);
                button.text = option.DisplayName;
            }
        }

        private void ShowHeroOverview(string heroId, Vector2 position)
        {
            var texture = Resources.Load<Texture2D>("Private/HeroSelection/" + heroId);
            if (texture == null) { HideHeroOverview(); return; }
            var bounds = Root.contentRect;
            var width = Mathf.Min(744f, bounds.width - 32f, (bounds.height - 32f) * texture.width / texture.height);
            var height = width * texture.height / texture.width;
            var local = Root.WorldToLocal(position);
            heroOverview.image = texture;
            heroOverview.style.width = width;
            heroOverview.style.height = height;
            heroOverview.style.left = Mathf.Clamp(local.x + 20f, 16f, bounds.width - width - 16f);
            heroOverview.style.top = Mathf.Clamp(local.y - height - 16f, 16f, bounds.height - height - 16f);
            heroOverview.style.display = DisplayStyle.Flex;
            // Milestone 8.76, the user: "make sure pick card is in the front". The card is the last
            // child of this view AND this view is raised to the front of whatever it is mounted on
            // every time the card opens, so nothing a host document adds beside the lobby later can
            // stand between a player and the card they are hovering.
            heroOverview.BringToFront();
            if (Root.parent != null)
            {
                Root.BringToFront();
            }
        }

        private void HideHeroOverview()
        {
            heroOverview.style.display = DisplayStyle.None;
            heroOverview.image = null;
        }

        private VisualElement BuildSeatRow(PrivateLobbySeatRow row, bool offerPick)
        {
            var element = new VisualElement { name = SeatRowName };
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.marginBottom = 4f;
            element.Add(Cell(row.SeatLabel, 320f, "gd-lobby-seat-name"));
            element.Add(Cell(row.HeroLabel, 300f, "gd-lobby-seat-hero"));
            element.Add(Cell(row.TeamLabel, 200f, "gd-lobby-seat-team"));
            element.Add(Cell(row.OccupancyLabel, 160f, "gd-lobby-seat-occupancy"));
            element.Add(Cell(row.ReadinessLabel, 200f, "gd-lobby-seat-readiness"));
            element.Add(Cell(row.IsOwnSeat ? "YOU" : string.Empty, 90f, "gd-lobby-seat-own"));
            if (offerPick)
            {
                // 8.35z54. One press takes the seat: it names the row and sends the claim,
                // where the old entry-form button only remembered a seat for a JOIN that had not
                // happened yet. The press goes through the presenter like every other control, so
                // the host's answer - granted, or "that seat is not free" - is what the player is
                // told.
                var pick = new Button(() =>
                {
                    PickSeat(row.Seat);
                    ControlPressed?.Invoke(PrivateLobbyControl.ClaimSeat, CurrentEntry());
                })
                {
                    name = SeatPickName,
                    text = PrivateLobbyLabels.Control(PrivateLobbyControl.ClaimSeat)
                };
                pick.style.minWidth = 120f;
                pick.style.height = 40f;
                ApplyButtonChrome(pick);
                element.Add(pick);
            }

            return element;
        }

        private Button BuildControl(PrivateLobbyControl control)
        {
            var button = new Button(() =>
                ControlPressed?.Invoke(control, CurrentEntry()))
            {
                name = ControlName,
                text = PrivateLobbyLabels.Control(control)
            };
            button.style.minWidth = 220f;
            button.style.height = 56f;
            button.style.marginRight = 10f;
            button.style.fontSize = 20f;
            ApplyButtonChrome(button);
            return button;
        }

        /// <summary>
        /// Milestone 8.35z8 (the EIGHTEENTH amendment). The lobby's own controls drew Unity's stock
        /// button skin - the one place in this HUD family that had never adopted
        /// <see cref="MatchHudTheme"/>'s dark chip language at all, which is exactly what read as
        /// "pasted over" a hand-styled dark panel. This gives HOST/JOIN/FILL/LEAVE and a seat's PICK
        /// the same background, border, corner radius and press/hover motion every match-HUD
        /// control now shares, through the same <see cref="HudElements.ApplyButtonMotion"/> this
        /// file borrows rather than re-implements.
        /// </summary>
        /// <remarks>
        /// <para>Milestone 8.35z58 also routes <see cref="BuildHeroOption"/> through this -
        /// that control was added in 8.35z37, after this method already existed, and was missed at
        /// the time. It is the ONE definition every lobby control's chrome reads from; a future
        /// control that draws its own <see cref="Button"/> should call this rather than re-stating
        /// the background/radius/motion literals.</para>
        /// <para>The SAME milestone also strengthened what this draws, having traced that 8.35z8's
        /// treatment DID reach this control at runtime but read as almost nothing against the
        /// lobby's own near-black panel: the idle chip is brighter, the corner radius now matches
        /// the top strip/log-tray band the user has already accepted live
        /// (<see cref="MatchHudTheme.BandCornerRadius"/>), and the border is the theme's own
        /// <see cref="MatchHudTheme.ButtonBorder"/> rather than a private literal.</para>
        /// </remarks>
        private static void ApplyButtonChrome(VisualElement button)
        {
            button.style.color = PanelText;
            button.style.backgroundColor = MatchHudTheme.ButtonIdleBackground;
            HudElements.Border(button, PanelEdge, 1f, MatchHudTheme.ButtonCornerRadius);
            HudElements.ApplyButtonMotion(button, tintBackground: true);
        }

        private static Label Caption(string text, int fontSize, string name)
        {
            var label = new Label(text) { name = name };
            label.style.color = PanelText;
            label.style.fontSize = fontSize;
            label.style.marginTop = 2f;
            label.style.marginBottom = 2f;
            return label;
        }

        private static Label Cell(string text, float width, string name)
        {
            var label = Caption(text, 18, name);
            label.style.width = width;
            label.style.minWidth = width;
            return label;
        }

        private static TextField Field(string label, string value, string name)
        {
            var field = new TextField(label) { name = name, value = value };
            field.style.marginTop = 2f;
            field.style.marginBottom = 2f;
            field.style.width = 620f;
            field.labelElement.style.color = PanelText;
            field.labelElement.style.minWidth = 200f;
            return field;
        }
    }

    /// <summary>
    /// THIRTEENTH amendment. What the host asks for when it spawns one test-driver child.
    /// </summary>
    internal sealed class TestDriverSpawnRequest
    {
        public TestDriverSpawnRequest(
            SeatId seat, string address, int port, string sessionKey,
            double? thinkSecondsOverride = null, int parentProcessId = 0,
            string matchFormatId = null)
        {
            Seat = seat;
            Address = address;
            Port = port;
            SessionKey = sessionKey;
            ThinkSecondsOverride = thinkSecondsOverride;
            ParentProcessId = parentProcessId;
            MatchFormatId = matchFormatId ?? string.Empty;
        }

        public SeatId Seat { get; }
        public string Address { get; }
        public int Port { get; }
        public string SessionKey { get; }

        /// <summary>
        /// PLAYTEST FAILURE #2 fix (lobby recut). The printed format the HOST composed, so the
        /// spawned child composes the SAME one at Awake rather than <see cref="MatchFormat.Default"/>
        /// (3v3 Quick). Before this, FILL WITH BOTS spawned every child on the default 3v3 and a host
        /// playing any other format refused each one <see cref="SessionRejectionReason.MatchFormatMismatch"/>
        /// and timed it out - so "fill with bots" was silently impossible on a 2v2 table, the same
        /// join-time format seam the human joiner hits. Empty means "let the child use the default",
        /// kept so a host that has somehow not composed a plan spawns exactly what it did before.
        /// </summary>
        public string MatchFormatId { get; }

        /// <summary>
        /// Unset by default, so a real solo session's spawned drivers keep
        /// <see cref="PrivateSeatDriverOptions.DefaultThinkSeconds"/>'s human-followable pace. Set
        /// only by <see cref="ReadThinkSecondsOverride"/> reading the one environment variable the
        /// automated Solo cohort uses to make the same real path finish in a bounded regression
        /// window instead of the many minutes six-players'-worth of human-paced bots would take.
        /// </summary>
        public double? ThinkSecondsOverride { get; }

        /// <summary>
        /// 8.35t corrective. The OS process id of the host that is about to spawn this child - the
        /// one fact a watchdog on the child's own side needs to notice its parent is gone even when
        /// the parent never got the chance to run its own <c>Leave()</c>/<c>OnDestroy()</c> teardown
        /// (a crash, a closed window, a task-killed process). Zero means "no parent to watch",
        /// kept non-fatal so a manually-launched driver (outside any host's own spawn) still runs.
        /// </summary>
        public int ParentProcessId { get; }
    }

    /// <summary>
    /// THIRTEENTH amendment. The one seam allowed to know an OS process exists, so the spawning
    /// decision - which seats, what each gets told - is testable without ever starting a real
    /// process; an Edit Mode fixture injects a fake here instead (Play Mode fixtures cannot: this
    /// assembly's own <c>InternalsVisibleTo</c> stops at the Edit Mode test assembly by design).
    /// </summary>
    internal interface ITestDriverProcessLauncher
    {
        System.Diagnostics.Process Launch(TestDriverSpawnRequest request);
    }

    /// <summary>
    /// Spawns another copy of THIS running executable as a real OS child process, exactly the shape
    /// <c>Invoke-PrivateOnlineRehearsal.ps1</c> already proves for the CI cohorts - reused as a
    /// pattern rather than by literally calling out to that script, which does not exist inside a
    /// shipped player. The endpoint and key travel only through the child's environment block, the
    /// same privacy rule the rehearsal runner and the rehearsal driver both already follow, and a
    /// distinct <c>-logFile</c> per child is load-bearing: several copies of the same build running
    /// at once would otherwise all fight over one default log path.
    /// </summary>
    internal sealed class RealTestDriverProcessLauncher : ITestDriverProcessLauncher
    {
        public System.Diagnostics.Process Launch(TestDriverSpawnRequest request)
        {
            var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                throw new InvalidOperationException("could not resolve this process's own executable path");
            }

            var logDirectory = System.IO.Path.Combine(
                UnityEngine.Application.persistentDataPath, "TestDrivers");
            System.IO.Directory.CreateDirectory(logDirectory);
            var logPath = System.IO.Path.Combine(
                logDirectory, "test-driver-" + request.Seat.Value + ".log");

            var arguments = new List<string>
            {
                GuardsDigital.Network.PrivateSessionLaunchOptions.LobbyFlag,
                PrivateSeatDriverOptions.RoleFlag,
                PrivateSeatDriverOptions.SeatFlag, Quote(request.Seat.Value),
                "-batchmode", "-nographics",
                "-logFile", Quote(logPath)
            };
            // PLAYTEST FAILURE #2 fix (lobby recut). Hand the child the HOST's format so it composes
            // the same printed setup at Awake and its admission tuple matches - the flag the ELEVENTH
            // amendment already accepts beside every role, including the lobby the child runs. Without
            // it the child booted the default 3v3 and a 2v2 host refused it MatchFormatMismatch.
            if (!string.IsNullOrEmpty(request.MatchFormatId))
            {
                arguments.Add(GuardsDigital.Network.PrivateSessionLaunchOptions.MatchFormatFlag);
                arguments.Add(Quote(request.MatchFormatId));
            }

            if (request.ThinkSecondsOverride.HasValue)
            {
                arguments.Add(PrivateSeatDriverOptions.ThinkSecondsFlag);
                arguments.Add(request.ThinkSecondsOverride.Value.ToString(CultureInfo.InvariantCulture));
            }

            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = string.Join(" ", arguments)
            };
            info.EnvironmentVariables[PrivateSeatDriverOptions.AddressEnvironmentVariable] = request.Address;
            info.EnvironmentVariables[PrivateSeatDriverOptions.PortEnvironmentVariable] =
                request.Port.ToString(CultureInfo.InvariantCulture);
            info.EnvironmentVariables[PrivateSeatDriverOptions.KeyEnvironmentVariable] = request.SessionKey;
            if (request.ParentProcessId > 0)
            {
                info.EnvironmentVariables[PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable] =
                    request.ParentProcessId.ToString(CultureInfo.InvariantCulture);
            }

            return System.Diagnostics.Process.Start(info);
        }

        private static string Quote(string value) => "\"" + (value ?? string.Empty) + "\"";
    }

    /// <summary>
    /// Milestone 8.35a. The one process-level piece of the lobby that knows a transport exists:
    /// it owns the panel, opens and closes the socket a pressed control asks for, pumps it into
    /// the session state machines on this thread, and hands the presenter a redraw when something
    /// a player can see has changed.
    /// </summary>
    /// <remarks>
    /// <para>Attached only when the player was launched with the lobby flag, exactly as the host
    /// and client drivers are: a normally launched build carries no lobby at all. The two
    /// command-line drivers are untouched - this is an additional interactive path over the same
    /// session layer, and the rehearsal script's marker and exit-code contract belongs to them.</para>
    /// <para>It composes a session but never the match: the authority it hands
    /// <see cref="PrivateSessionHost"/> is the one the scene already composed, and nothing here
    /// submits, executes or restores anything.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateLobbyDriver : MonoBehaviour
    {
        public const string MatchStatusName = "gd-online-match-status";
        /// <summary>The lobby draws above the match HUD, which sorts at 100.</summary>
        private const float LobbySortingOrder = 200f;

        private const int ConnectBudgetSeconds = 5;

        /// <summary>
        /// The seam milestone 8.34's real-socket reconnect fixture recorded as owed here: a
        /// client machine's outbox is fixed at construction, and the socket it was built over is
        /// exactly what a reconnect replaces, so a returning client answers the second challenge
        /// down the first dead socket unless the outbox can be swapped under it.
        /// </summary>
        private sealed class SwappableClientOutbox : IPrivateSessionClientOutbox
        {
            public TcpSessionClientTransport Live { get; set; }

            public void Send(byte[] frame) => Live?.Send(frame);

            public void Close() => Live?.Close();
        }

        private readonly SwappableClientOutbox clientOutbox = new SwappableClientOutbox();
        private MatchSceneCompositionRoot compositionRoot;
        private UIDocument document;
        private PanelSettings panelSettings;
        private PrivateLobbyUnityView view;
        private PrivateLobbyPresenter presenter;
        private TcpSessionListener listener;
        private TcpSessionClientTransport transport;
        private PrivateSessionHost session;
        private HostConsoleReader console;
        private PrivateSessionClient machine;
        private SeatId joinedSeat;
        private string sessionKey = string.Empty;
        private PrivateSessionEndpoint dialled;
        private string signature = string.Empty;

        /// <summary>
        /// PLAYTEST FAILURE #2 fix (lobby recut). Set once per join attempt, so a joiner that had to
        /// inherit the host's format and re-dial does it EXACTLY ONCE - a second format refusal after
        /// inheriting is a genuine mismatch (a custom board, a build difference) rather than the
        /// default-3v3-versus-a-2v2-host case this recovers, and must be left to fail by name rather
        /// than looped. Cleared at the top of every fresh <see cref="BeginJoining"/>.
        /// </summary>
        private bool formatInheritanceAttempted;

        /// <summary>
        /// PLAYTEST FAILURE #2 fix. The last lobby pick refusal already surfaced, so a REFUSED hero
        /// pick - which leaves the client Seated rather than Rejected and was therefore logged NOWHERE
        /// before this, the silence the diagnosis names as why this shipped twice - is written to the
        /// log once per distinct refusal rather than every frame or never.
        /// </summary>
        private string lastSurfacedLobbyRefusal = string.Empty;

        private GuardsDigital.Application.Bots.PrivateSessionBotFleet botFleet;
        private string reportedBotFailure = string.Empty;
        private readonly HashSet<SeatId> testDriverSeats = new HashSet<SeatId>();


        internal IReadOnlyCollection<SeatId> TestDriverSeatsForTests => testDriverSeats;

        /// <summary>Milestone 8.35k. So one refusal is logged once rather than every redraw.</summary>
        private bool refusalReported;
        private Label matchStatus;
        private bool onlineHudAttached;
        private bool outcomeReported;
        private PrivateOnlineEvidenceFile evidence;
        private bool evidenceUnavailable;

        /// <summary>
        /// Host-only event observation for the opt-in build rehearsal. The ordinary lobby does
        /// not subscribe; exposing the already privacy-bounded records keeps the automation from
        /// reaching into match authority or transport internals.
        /// </summary>
        internal event Action<PrivateSessionEventRecord> HostEventObserved;

        public void Configure(MatchSceneCompositionRoot root)
        {
            compositionRoot = root;
            // Milestone 8.31a's reasoning, unchanged: a lobby that pauses on focus loss stops
            // pumping the session it is showing, and six windows on one machine are mostly
            // unfocused. This one process only; a normal launch never reaches this line.
            UnityEngine.Application.runInBackground = true;
        }

        /// <summary>The presenter this driver drives, for a journey that composes it deliberately.</summary>
        public PrivateLobbyPresenter Presenter => presenter;

        /// <summary>The view this driver draws into.</summary>
        public PrivateLobbyUnityView View => view;

        internal PrivateSessionClient RehearsalClient => machine;

        internal PrivateSessionHost RehearsalHost => session;

        internal bool RehearsalHudAttached => onlineHudAttached;

        /// <summary>
        /// 8.35t corrective. Set the instant <see cref="BeginJoining"/> takes either of its own
        /// silent early returns, cleared the instant a join is attempted again: the one thing a
        /// spawned <see cref="PrivateSeatDriver"/> could not previously observe about its own
        /// "successful" Join press - <see cref="PrivateLobbyPresenter.Press"/> returns true the
        /// moment the control was OFFERED, which says nothing about whether the join it triggered
        /// actually reached anyone.
        /// </summary>
        internal string LastJoinFailure { get; private set; }

        private void Start()
        {
            if (compositionRoot?.Runtime == null)
            {
                PrivateOnlineMarkers.Fail("the lobby driver was attached without a runtime");
                return;
            }

            BuildPanel();
            view = new PrivateLobbyUnityView();
            document.rootVisualElement?.Add(view.Root);
            // Item 3 of the 2026-09-06 log drop-down refinements. This notice used to be a 30%-wide,
            // 18pt slab pinned at top:92 in a mixed percent/reference geometry - which put it across
            // x 896..1664, y 92..136 at the reference viewport, straight over the open match log's
            // own header band (x 960..1600, y 96..416). It is now demoted on all three counts the
            // item names: it is HIDDEN outright while the connection is healthy, it is placed by
            // ConfiguredMatchHudRenderer.CalculateConnectionBannerRect clear of both the narration
            // bar and the drop-down's full open extent, and it wears the HUD's own panel chrome at
            // the strip's own type size rather than an ad-hoc dark slab twice everything else's.
            matchStatus = new Label { name = MatchStatusName };
            matchStatus.style.position = Position.Absolute;
            matchStatus.style.unityTextAlign = TextAnchor.MiddleCenter;
            matchStatus.style.fontSize = ConnectionBannerFontSize;
            matchStatus.style.color = MatchHudTheme.PrimaryText;
            matchStatus.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.Border(
                matchStatus,
                MatchHudTheme.PanelBorder,
                MatchStatusBarUnityView.BorderWidth,
                MatchStatusBarUnityView.CornerRadius);
            matchStatus.style.display = DisplayStyle.None;
            matchStatus.pickingMode = PickingMode.Ignore;
            document.rootVisualElement?.Add(matchStatus);
            LayoutMatchStatus();
            presenter = new PrivateLobbyPresenter(view);
            view.ControlPressed += (control, entry) => presenter.Press(control, entry);
            // Milestone 8.35z37. Clicking a hero only says which one the next PICK HERO
            // press sends; the press is what reaches the host.
            view.HeroHighlighted += heroId => presenter.SelectedHeroIdValue = heroId;
            presenter.HostRequested += BeginHosting;
            presenter.JoinRequested += BeginJoining;
            presenter.LeaveRequested += Leave;
            presenter.ReconnectRequested += Redial;
            presenter.FillWithTestDriversRequested += FillEmptySeatsWithTestDrivers;
            presenter.MatchFormatChangeRequested += CycleMatchFormat;
            // 8.35z54. The four printed setups as named buttons.
            presenter.MatchSetupChoiceRequested += ChooseMatchSetup;
            presenter.ShowEntry(LocalSeats(), string.Empty, ComposedFormatLabel());
            PrivateOnlineMarkers.Log("LOBBY_BEGIN");
        }

        private void Update()
        {
            if (presenter == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (session != null && listener != null)
            {
                // Milestone 8.35o. The dedicated host's one typed command, "release <seat>" - the
                // escape hatch milestone 8.35k built but left with no way for a human to reach it.
                // The reader owns its own background thread, so draining it every frame here never
                // blocks this serve loop.
                while (console != null && console.TryDequeue(out var consoleLine))
                {
                    HostConsoleCommands.Dispatch(consoleLine, session);
                }

                foreach (var hostEvent in listener.Drain())
                {
                    switch (hostEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            session.OnConnected(hostEvent.Ordinal, now.Ticks);
                            break;
                        case TransportEventKind.Frame:
                            session.OnFrame(hostEvent.Ordinal, hostEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            session.OnStreamRejected(hostEvent.Ordinal, hostEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            session.OnDisconnected(hostEvent.Ordinal);
                            break;
                    }
                }

                botFleet?.Tick(now.Ticks);
                if (botFleet != null && botFleet.LastFailure != reportedBotFailure)
                {
                    reportedBotFailure = botFleet.LastFailure;
                    if (reportedBotFailure.Length > 0)
                    {
                        presenter.SetTestDriverSeats(testDriverSeats, reportedBotFailure);
                        PrivateOnlineMarkers.Fail(reportedBotFailure);
                    }
                }
                session.Sweep(now.Ticks);
                foreach (var record in session.DrainEvents())
                {
                    PrivateOnlineMarkers.LogHostEvent(record, session);
                    HostEventObserved?.Invoke(record);
                }

            }

            if (machine != null && transport != null)
            {
                foreach (var clientEvent in transport.Drain())
                {
                    switch (clientEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            machine.OnConnected();
                            break;
                        case TransportEventKind.Frame:
                            machine.OnFrame(clientEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            machine.OnStreamRejected(clientEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            machine.OnDisconnected();
                            break;
                    }
                }
            }

            // PLAYTEST FAILURE #2 fix (lobby recut). A joiner refused MatchFormatMismatch inherits
            // the host's format and re-dials, once. This is checked before anything else reads the
            // client's state so a joiner never lingers on a terminal Rejected the way the shipped
            // build's every 2v2 joiner did.
            if (TryInheritHostFormatAndRejoin())
            {
                return;
            }

            SurfaceLobbyPickRefusal();

            EnsureEvidence();
            ReportOutcomeOnce();

            var current = Signature();
            if (!string.Equals(current, signature, StringComparison.Ordinal))
            {
                signature = current;
                // Milestone 8.35k. The diagnostic gap that made this corrective take a day to
                // diagnose from a returned playtest log: a refused client left the reason nowhere.
                // The reason is the host's own named refusal and carries nothing private - the
                // capability is not a parameter it can arrive through.
                if (machine != null &&
                    machine.State == PrivateSessionClientState.Rejected &&
                    !refusalReported)
                {
                    refusalReported = true;
                    PrivateOnlineMarkers.Log("LOBBY_REFUSED reason=" + machine.RejectionReason);
                }
                if (!onlineHudAttached && machine != null && machine.HasMatchStarted &&
                    machine.State == PrivateSessionClientState.Seated &&
                    machine.LatestProjection != null)
                {
                    // Milestone 8.35z37. A CLIENT recomposes too, and it has to. Its board
                    // is its OWN composed match updated by the host's projections, so a host that
                    // composed a chosen line-up while this process kept the printed one would leave
                    // this player looking at the wrong heroes on every seat. The line-up is read off
                    // the roster - which has carried a hero id per row since milestone 8.31 and now
                    // says which of them were chosen - so this needs no message of its own.
                    //
                    // It happens BEFORE the HUD is attached, because attaching binds the surfaces to
                    // the runtime and a recomposition replaces it. Null means the roster does not
                    // name a line-up this build can compose, and the honest answer is to leave this
                    // process on the match it has rather than compose something else and call it the
                    // same match.
                    // Milestone 8.35z62. THE TEAM-SIZE GUARD BELOW USED TO FAIL SILENTLY, and that
                    // silence is the second half of the same defect 8.35z62 opens with. When the
                    // host's line-up did not match this process's own composed team size, the
                    // recomposition was skipped and the very next line attached the HUD TO THE
                    // PROVISIONAL RUNTIME ANYWAY - so the board rebuilt from the host's projections
                    // while the hero rails and the hand fan kept drawing the printed demo six, and
                    // the hand stayed Director-scoped because the seat was bound onto a runtime the
                    // chosen line-up had never reached. That is exactly what the 2026-09-08 session
                    // reported: correct tokens on the map, six strangers in the trays.
                    //
                    // The team size can differ for a real reason - milestone 8.35z57's own finding is
                    // that a joiner composes a format before it can know the host's - so the answer
                    // is to ADOPT THE HOST'S FORMAT rather than to skip. The roster names the
                    // line-up, and a line-up names its team size, so the format is derivable here
                    // without a new message, exactly as z57 derives it from the plaintext refusal.
                    var lineup = machine.ComposedLineup();
                    if (session != null)
                    {
                        // Milestone 8.36g1. The session called this same composition root before it
                        // announced MatchStarted and replaced its authority with the returned host.
                        // Re-running the joiner's local-copy composition here would create a fresh
                        // version-zero twin and point the hosting player's HUD away from the match
                        // every remote and bot is actually playing.
                        PrivateOnlineMarkers.Log(
                            "LOBBY_HOST_RUNTIME_RETAINED seat=" + machine.TargetSeat.Value);
                    }
                    else if (lineup != null && compositionRoot.ComposedPlan != null)
                    {
                        var plan = compositionRoot.ComposedPlan;
                        if (lineup.HeroesPerTeam != plan.Format.HeroesPerTeam)
                        {
                            var hostFormat = FindFormatForTeamSize(
                                lineup.HeroesPerTeam, plan.Format);
                            if (hostFormat == null)
                            {
                                // Nothing this build can compose seats that line-up. Refuse LOUDLY
                                // and do not attach: a HUD bound to a match whose seats disagree with
                                // the host's is the state this whole block exists to make
                                // unreachable.
                                PrivateOnlineMarkers.Log(
                                    "LOBBY_TRANSITION_REFUSED reason=UncomposableLineup heroesPerTeam=" +
                                    lineup.HeroesPerTeam.ToString(CultureInfo.InvariantCulture));
                                RenderMatchStatus();
                                return;
                            }

                            PrivateOnlineMarkers.Log(
                                "LOBBY_FORMAT_INHERITED_AT_TRANSITION format=" + hostFormat.Id);
                            plan = MatchCompositionPlan.Printed(hostFormat);
                        }

                        compositionRoot.Recompose(plan.WithLineup(lineup));
                    }
                    else if (lineup == null)
                    {
                        // The roster does not name a line-up this build can read. This STILL
                        // attaches, deliberately and unchanged: the seam's original remark is that
                        // the honest answer is to leave this process on the match it has rather than
                        // compose something else and call it the same match, and refusing to attach
                        // here would strand the player in a lobby whose match has already started.
                        // What is new is that it is no longer SILENT - a returned playtest log can
                        // now distinguish "the trays are wrong because the line-up never arrived"
                        // from every other reason they might be wrong, which is precisely the
                        // distinction the 2026-09-08 report could not be diagnosed from.
                        PrivateOnlineMarkers.Log(
                            "LOBBY_TRANSITION_LINEUP_ABSENT seat=" + machine.TargetSeat.Value);
                    }

                    compositionRoot.Runtime.AttachOnlineClient(
                        machine,
                        exposeHostDirectorTools: session != null,
                        republishAfterHostDebugChange: session == null
                            ? (Action)null
                            : session.RepublishProjections);
                    onlineHudAttached = true;
                    PrivateOnlineMarkers.Log(
                        "LOBBY_MATCH_HUD seat=" + machine.TargetSeat.Value +
                        " version=" + machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture));
                }

                presenter.Refresh();
                if (onlineHudAttached)
                {
                    compositionRoot.Runtime.RefreshAll();
                }

                RenderMatchStatus();
            }
        }

        /// <summary>
        /// Milestone 8.36. Writes this process's own ending exactly once, so an invited playtest's
        /// returned logs can be compared with the host's instead of with what a player remembers.
        /// </summary>
        /// <remarks>
        /// The rehearsal driver wrote its own outcome marker and will not be attached when six
        /// people are playing, which left the ordinary lobby path saying nothing at all about the
        /// ending it reached. The line is <see cref="PrivateOnlineDiagnostics.OutcomeLine"/>'s, so
        /// what it may contain is decided by that signature rather than here, and the host form
        /// omits the seat because a dedicated host holds none.
        /// </remarks>
        private void ReportOutcomeOnce()
        {
            if (outcomeReported || compositionRoot?.Runtime == null)
            {
                return;
            }

            string line;
            if (session != null)
            {
                var authoritative = compositionRoot.Runtime.Host.Projection;
                line = PrivateOnlineDiagnostics.OutcomeLine(
                    string.Empty, authoritative.Version, authoritative.Outcome);
            }
            else if (onlineHudAttached && machine?.LatestProjection != null)
            {
                line = PrivateOnlineDiagnostics.OutcomeLine(
                    machine.TargetSeat.Value,
                    machine.LatestProjection.Version,
                    compositionRoot.Runtime.ReadModel.Outcome);
            }
            else
            {
                return;
            }

            if (line.Length == 0)
            {
                return;
            }

            outcomeReported = true;
            PrivateOnlineMarkers.Log(line);
            if (evidence != null)
            {
                try
                {
                    if (session != null)
                    {
                        var bots = new HashSet<string>(botFleet == null ? Array.Empty<string>() :
                            botFleet.Seats.Select(seat => seat.Value));
                        evidence.Complete(compositionRoot.Runtime.Host.Projection.Version,
                            compositionRoot.Runtime.Host.Projection.Outcome,
                            session.Roster.Where(row => !bots.Contains(row.Seat.Value)).Select(row => row.Seat.Value), bots);
                    }
                    else
                        evidence.Complete(machine.LatestProjection.Version, compositionRoot.Runtime.ReadModel.Outcome);
                }
                catch (System.IO.IOException) { EvidenceFailed(); }
                catch (UnauthorizedAccessException) { EvidenceFailed(); }
            }
        }

        private void EnsureEvidence()
        {
            if (evidence != null || evidenceUnavailable || compositionRoot?.Runtime == null) return;
            if (session == null && (machine == null || machine.State != PrivateSessionClientState.Seated)) return;
            try
            {
                var directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                    "..", UnityEngine.Application.isEditor ? "tmp" : "Logs", "online-runs"));
                evidence = new PrivateOnlineEvidenceFile(directory, compositionRoot.Runtime.SessionIdentity,
                    session != null ? session.SessionCorrelation : machine.SessionCorrelation,
                    session != null ? PrivateOnlineEvidenceRole.Host : PrivateOnlineEvidenceRole.Client,
                    session != null ? "" : machine.TargetSeat.Value);
                PrivateOnlineMarkers.Log("EVIDENCE_BEGIN run=" + evidence.RunId + " role=" + evidence.Role +
                    " " + PrivateOnlineMarkers.Describe(compositionRoot.Runtime.SessionIdentity));
            }
            catch (System.IO.IOException) { EvidenceFailed(); }
            catch (UnauthorizedAccessException) { EvidenceFailed(); }
        }

        private void EvidenceFailed()
        {
            evidenceUnavailable = true;
            Debug.LogWarning("GUARDS_ONLINE: EVIDENCE_UNAVAILABLE (local capture failed)");
        }

        private void CloseEvidence()
        {
            try { evidence?.Dispose(); }
            catch (System.IO.IOException) { EvidenceFailed(); }
            catch (UnauthorizedAccessException) { EvidenceFailed(); }
            evidence = null;
            evidenceUnavailable = false;
        }

        /// <summary>
        /// Milestone 8.35k. Where this machine keeps its road back between processes.
        /// </summary>
        /// <remarks>
        /// `persistentDataPath` is per-user and outside the installation, so a packaged build stays
        /// read-only and a returned playtest log bundle sweeps up nothing from here. The Application
        /// assembly is Unity-free, so it takes the directory rather than discovering it - this is the
        /// one place that knows Unity is underneath.
        /// </remarks>
        private static IRetainedSeatCapabilityStore RetainedCapabilities() =>
            new FileRetainedSeatCapabilityStore(UnityEngine.Application.persistentDataPath);

        /// <summary>
        /// Everything a player could see, as one string. The lobby redraws when this moves and not
        /// otherwise, so a primitive rebuild-everything render costs one frame per real change
        /// rather than one per frame.
        /// </summary>
        private string Signature()
        {
            var builder = new StringBuilder();
            if (machine != null)
            {
                builder.Append("client:").Append(machine.State).Append(':')
                    .Append(machine.RejectionReason).Append(':')
                    .Append(machine.HasMatchStarted ? '1' : '0').Append(':')
                    .Append(machine.IsWaitingForHost ? '1' : '0').Append(':')
                    .Append(machine.IsHostResponseDelayed ? '1' : '0').Append(':')
                    .Append(machine.MatchFeedback).Append(':')
                    .Append(machine.LatestProjection?.Version ?? -1).Append(':')
                    .Append(machine.ProjectionRevision).Append(':')
                    .Append(machine.LastSubmissionResult?.SubmissionId ?? string.Empty).Append(':')
                    .Append(machine.LastAgreementResult?.SubmissionId ?? string.Empty);
                // 8.35z54. THE HERO AND THE CHOSEN FLAG BELONG IN THIS STRING, and leaving
                // them out is why an accepted pick "didnt do nothing" on the user's screen. It is
                // the whole redraw condition - the lobby rebuilds when it moves and at no other
                // time - and it used to name a row's seat, occupancy and readiness only, while a
                // pick moves exactly the two it did not name. Measured on the shipped build: the
                // string was character-for-character identical either side of an accepted pick, on
                // the picker's own client and on the host.
                //
                // It is now `PrivateLobbyModel.RosterSignature`, stated once beside the rows
                // themselves, so the same blindness cannot come back on one of the two branches
                // and so it is provable without a running scene.
                builder.Append(PrivateLobbyModel.RosterSignature(machine.Roster));
            }
            else if (session != null)
            {
                builder.Append("host:").Append(session.ClaimedSeatCount).Append(':')
                    .Append(session.ReadySeatCount).Append(':')
                    .Append(session.HasMatchStarted ? '1' : '0')
                    // The host's own screen had the same blindness for the same reason, and it
                    // draws the selection too - so it watches the roster rather than three counts.
                    .Append(PrivateLobbyModel.RosterSignature(session.Roster));
            }
            else
            {
                builder.Append("entry");
            }

            return builder.ToString();
        }

        /// <summary>
        /// The connection banner's own type size. The strip's identity row reads at 12-13 and the
        /// narration at its own larger size; this sits just above the labels so an anomaly is
        /// noticed without competing with the line the whole HUD is built around - item 3's
        /// "demoted", in one number (it was 18).
        /// </summary>
        private const float ConnectionBannerFontSize = 14f;

        /// <summary>
        /// Puts the banner where item 3 asks for it: clear of the narration bar and of the match-log
        /// drop-down's full open extent, in the panel's own reference units.
        /// </summary>
        /// <remarks>
        /// The lobby's <c>UIDocument</c> loads the SAME <c>PanelSettings</c> the HUD does (see
        /// <c>BuildPanel</c>), so the two coordinate spaces are one space and this rectangle can be
        /// stated in the HUD's own terms rather than in the percentages it used to mix with them.
        /// Re-stated on every status render, so a window resized between two states still places it
        /// correctly; at every supported viewport the rectangle is the same one anyway, because both
        /// terms it derives from are capped well inside the panel.
        /// </remarks>
        private void LayoutMatchStatus()
        {
            if (matchStatus == null)
            {
                return;
            }

            var viewport = MatchHudPanelScaling.CurrentReferenceViewport();
            var rect = ConfiguredMatchHudRenderer.CalculateConnectionBannerRect(viewport.x, viewport.y);
            matchStatus.style.left = rect.x;
            matchStatus.style.top = rect.y;
            matchStatus.style.width = rect.width;
            matchStatus.style.height = rect.height;
        }

        private void RenderMatchStatus()
        {
            if (matchStatus == null)
            {
                return;
            }

            // Item 3 of the 2026-09-06 log drop-down refinements: "a HEALTHY connection is the
            // default state and earns no screen". The decision itself is MatchConnectionBanner's, so
            // that every state it hides and every state it surfaces is provable in an Edit Mode
            // fixture rather than only in a running six-client session.
            var banner = machine == null
                ? MatchConnectionBannerViewModel.Hidden
                : MatchConnectionBanner.For(
                    onlineHudAttached,
                    machine.HasMatchStarted,
                    machine.State,
                    machine.IsReconnecting,
                    machine.LatestProjection != null,
                    machine.IsWaitingForHost,
                    machine.MatchFeedback,
                    machine.RejectionReason.ToString(),
                    machine.IsHostResponseDelayed);

            LayoutMatchStatus();
            matchStatus.style.display = banner.IsShown ? DisplayStyle.Flex : DisplayStyle.None;
            matchStatus.text = banner.Text;
        }

        /// <summary>
        /// Milestone 8.35z37. Which of rulebook pp.5-6's four printed setups is composed
        /// right now, as words - read off the composition rather than off an intention beside it.
        /// </summary>
        private string ComposedFormatLabel() =>
            compositionRoot?.ComposedPlan == null
                ? null
                : compositionRoot.ComposedPlan.Format.Id.ToUpperInvariant();

        /// <summary>
        /// Milestone 8.35z37. The minimal format picker: steps to the next of the four
        /// printed setups and RECOMPOSES the scene on it.
        /// </summary>
        /// <remarks>
        /// <para><b>Only on the entry form, and the guard is structural rather than a check on a
        /// button.</b> A listener means a session, a session means seats somebody may already hold,
        /// and the seats are counted from the format - so this refuses the moment a listener exists.
        /// The wire refuses it a second time: <c>PrivateSessionIdentity</c> rejects a peer on
        /// <c>MatchFormatMismatch</c>, so a host that changed format under a connected client would
        /// have refused that client anyway.</para>
        /// <para><b>Why cycling rather than a menu.</b> There are four printed setups and the lobby
        /// is deliberately primitive - milestone 8.35a's own non-goals - so one control that names
        /// what it will compose next is the whole feature. A menu is presentation this milestone
        /// does not owe.</para>
        /// <para>The recomposition is what makes the pick real: it rebuilds the board, the runtime
        /// and the identity tuple, so the seats the entry form then lists and the tuple a client
        /// checks both describe the match that will actually be played.</para>
        /// </remarks>
        private void CycleMatchFormat()
        {
            if (RefuseFormatChangeIfUnavailable())
            {
                return;
            }

            var formats = MatchFormat.All;
            var current = compositionRoot.ComposedPlan.Format;
            var index = 0;
            for (var candidate = 0; candidate < formats.Count; candidate++)
            {
                if (string.Equals(formats[candidate].Id, current.Id, StringComparison.Ordinal))
                {
                    index = candidate;
                    break;
                }
            }

            var next = formats[(index + 1) % formats.Count];
            compositionRoot.Recompose(MatchCompositionPlan.Printed(next));
            PrivateOnlineMarkers.Log("LOBBY_FORMAT format=" + next.Id);
            presenter.ShowEntry(LocalSeats(), string.Empty, ComposedFormatLabel());
        }

        /// <summary>
        /// 8.35z54. Composes the printed setup the host NAMED, by changing one half of it -
        /// the team size or the game length - and keeping the other.
        /// </summary>
        /// <remarks>
        /// <para>The four printed setups of pp.5-6 are two crossed pairs, so a named choice is a
        /// move along one axis rather than a jump to one of four ids: pressing 2V2 while the LONG
        /// game is composed gives the long 2v2, which is what a player who pressed one button
        /// expects and what a cycle could never promise.</para>
        /// <para>The guard is the one <see cref="CycleMatchFormat"/> already carries and for the
        /// same structural reason: a listener means seats somebody may hold, and the seats are
        /// counted from the format. Recomposing is what makes the choice real - it rebuilds the
        /// board, the runtime and the admission tuple, so the seats this form then lists and the
        /// tuple a client checks both describe the match that will actually be played.</para>
        /// </remarks>
        private void ChooseMatchSetup(PrivateLobbyControl control)
        {
            if (RefuseFormatChangeIfUnavailable())
            {
                return;
            }

            var current = compositionRoot.ComposedPlan.Format;
            var wantsHeroesPerTeam = control == PrivateLobbyControl.ChooseTwoVsTwo ? 2
                : control == PrivateLobbyControl.ChooseThreeVsThree ? 3
                : current.HeroesPerTeam;
            var wantsLong = control == PrivateLobbyControl.ChooseLongGame ||
                (control != PrivateLobbyControl.ChooseShortGame && IsLongGame(current));

            MatchFormat chosen = null;
            foreach (var candidate in MatchFormat.All)
            {
                if (candidate.HeroesPerTeam == wantsHeroesPerTeam &&
                    IsLongGame(candidate) == wantsLong)
                {
                    chosen = candidate;
                    break;
                }
            }

            if (chosen == null)
            {
                RefuseFormatChange(
                    "NoSuchPrintedSetup",
                    "No printed setup on pp.5-6 matches that choice, so nothing was changed.");
                return;
            }

            if (string.Equals(chosen.Id, current.Id, StringComparison.Ordinal))
            {
                // Milestone 8.35z62. Benign, but SAID. This used to return silently, and a silent
                // no-op is indistinguishable from a broken button to the person pressing it - which
                // is exactly the confusion the live 2026-09-08 session reported.
                presenter.ShowEntry(
                    LocalSeats(),
                    "Already playing " + chosen.Id + ".",
                    ComposedFormatLabel());
                return;
            }

            compositionRoot.Recompose(MatchCompositionPlan.Printed(chosen));
            PrivateOnlineMarkers.Log("LOBBY_FORMAT format=" + chosen.Id);
            presenter.ShowEntry(LocalSeats(), string.Empty, ComposedFormatLabel());
        }

        /// <summary>
        /// Milestone 8.35z62. The printed setup that seats a team of this size, keeping the game
        /// length already composed so only the axis the host actually disagrees on moves. Null when
        /// no printed setup on pp.5-6 seats that many, which is a refusal rather than a fallback.
        /// </summary>
        private static MatchFormat FindFormatForTeamSize(int heroesPerTeam, MatchFormat current)
        {
            var wantsLong = IsLongGame(current);
            MatchFormat sameLength = null;
            foreach (var candidate in MatchFormat.All)
            {
                if (candidate.HeroesPerTeam != heroesPerTeam)
                {
                    continue;
                }

                if (IsLongGame(candidate) == wantsLong)
                {
                    return candidate;
                }

                sameLength = sameLength ?? candidate;
            }

            return sameLength;
        }

        /// <summary>
        /// Milestone 8.35z62. Whether a format press can apply at all, and if not, SAYING SO.
        /// </summary>
        /// <remarks>
        /// <para><b>The defect this closes, from the user's own 2026-09-08 session.</b> Both format
        /// entry points opened with the same guard, and that guard <c>return</c>ed SILENTLY. A player
        /// who pressed HOST and then pressed 2V2 got no format change, no refusal, no marker and no
        /// line on screen - the host log from that session has NO <c>LOBBY_FORMAT</c> entry at all
        /// while the session went on to host the composed default. From the player's chair that is
        /// indistinguishable from a dead button, and it cost a whole playtest.</para>
        ///
        /// <para><b>The guard itself is CORRECT and is kept.</b> A session's seats are counted from
        /// the format, and every live connection, epoch and reconnect capability the host holds is
        /// keyed by seat, so a format changed after anybody can hold a seat would move the seats out
        /// from under them - <c>MatchSceneCompositionRoot.ComposeForLineup</c>'s own remark states
        /// this rule. What was wrong was never the refusal; it was the SILENCE. So this refuses on
        /// exactly the same condition and now names it, both to the log a returned playtest carries
        /// and to the entry form the player is looking at.</para>
        /// </remarks>
        private bool RefuseFormatChangeIfUnavailable()
        {
            if (compositionRoot?.ComposedPlan == null)
            {
                RefuseFormatChange(
                    "NoComposedMatch",
                    "The scene has not composed a match yet, so there is no setup to change.");
                return true;
            }

            if (listener != null || session != null || machine != null)
            {
                RefuseFormatChange(
                    "AlreadyHosting",
                    "The setup is fixed once hosting begins - the seats are counted from it. " +
                    "Leave the lobby to choose a different setup.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Milestone 8.35z62. One refusal, said in both places a refusal has to be said: the marker
        /// stream a returned playtest log is read from, and the entry form the player is looking at.
        /// </summary>
        private void RefuseFormatChange(string reason, string sentence)
        {
            PrivateOnlineMarkers.Log("LOBBY_FORMAT_REFUSED reason=" + reason);
            if (compositionRoot?.ComposedPlan == null)
            {
                presenter.ShowEntry(Array.Empty<SeatRosterEntry>(), sentence, string.Empty);
                return;
            }

            presenter.ShowEntry(LocalSeats(), sentence, ComposedFormatLabel());
        }

        /// <summary>
        /// Which half of a printed setup's id names the LONG game. The four ids are the two team
        /// sizes crossed with the two lengths, so the length is read off the id rather than kept as
        /// a second field this surface would have to hold in step with it.
        /// </summary>
        private static bool IsLongGame(MatchFormat format) =>
            format.Id.IndexOf("long", StringComparison.OrdinalIgnoreCase) >= 0;

        private IReadOnlyList<SeatRosterEntry> LocalSeats()
        {
            var rows = new List<SeatRosterEntry>();
            foreach (var seat in compositionRoot.Runtime.Host.Seats.Seats)
            {
                rows.Add(new SeatRosterEntry(
                    seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Open));
            }

            return rows;
        }

        private void BeginHosting(PrivateLobbyEntry entry)
        {
            if (!TryReadEndpoint(entry, out var endpoint, out var error))
            {
                presenter.ShowEntry(LocalSeats(), error);
                return;
            }

            // Milestone 8.35a1. The same rule the command-line host role has always obeyed, read
            // from the one place that now states it, and read before a listener exists: an unkeyed
            // session may bind only the loopback interface, because its link stays plaintext.
            if (PrivateSessionProtocol.BindRequiresSessionKey(
                    endpoint.IsLoopback, entry == null ? string.Empty : entry.SessionKey))
            {
                presenter.ShowEntry(
                    LocalSeats(), PrivateLobbyLabels.UnkeyedPublicBindRefusal);
                return;
            }

            try
            {
                listener = new TcpSessionListener();
                listener.Start(endpoint);
            }
            catch (Exception exception)
            {
                listener?.Dispose();
                listener = null;
                presenter.ShowEntry(
                    LocalSeats(), "The host could not bind: " + exception.GetType().Name + ".");
                return;
            }

            sessionKey = entry.SessionKey;
            var draftSeed = BitConverter.ToInt32(new CryptoNonceSource().NextNonce(), 0);
            botFleet = new GuardsDigital.Application.Bots.PrivateSessionBotFleet(
                compositionRoot.Runtime.Host, compositionRoot.Runtime.SessionIdentity,
                sessionKey, listener, new CryptoNonceSource(),
                () => compositionRoot.Runtime.ReadModel, compositionRoot.Runtime.ContentCatalogs, draftSeed: draftSeed);
            session = botFleet.Host;
            PrivateOnlineMarkers.Log("LOBBY_BOT_DRAFT_SEED seed=" + draftSeed.ToString(CultureInfo.InvariantCulture));

            // Milestone 8.35z61. The gameplay narration, on the lobby host too. The
            // 2026-09-06 playtest was hosted through THIS path, and a narration attached only to
            // the smoke driver would have been missing from exactly the session that needed it.
            session.AttachGameplayNarration(
                GameplayNarrationFile.TryOpen(),
                compositionRoot.Runtime.SessionIdentity.BuildIdentity,
                compositionRoot.Runtime.SessionIdentity.MapId);
            // Milestone 8.35z37. The seam, attached at the one moment it can be: the host
            // exists, so the selection stage its seats are built from exists, and nothing has been
            // admitted yet. From here the session owns WHEN the match is composed, and this scene
            // owns HOW - which is the whole inversion IDeferredMatchComposition exists for.
            session.Composition = compositionRoot;
            console = new HostConsoleReader();
            console.Start();
            signature = string.Empty;
            testDriverSeats.Clear();
            reportedBotFailure = string.Empty;
            joinedSeat = default;
            if (!PrivateSessionEndpoint.TryCreate(
                    endpoint.ResolveLocalDialAddress(), listener.BoundPort,
                    out dialled, out var localEndpointError))
            {
                AbortHostingStart("The hosting player could not open its local seat: " + localEndpointError);
                return;
            }

            machine = new PrivateSessionClient(
                compositionRoot.Runtime.SessionIdentity, joinedSeat, sessionKey,
                clientOutbox, new CryptoNonceSource(), retainedCapabilities: RetainedCapabilities());
            OpenTransport();
            if (!Dial())
            {
                AbortHostingStart("The hosting player could not join the local session.");
                return;
            }

            presenter.AttachHostingClient(session, machine, string.Empty);
            PrivateOnlineMarkers.Log(
                "LOBBY_HOSTING port=" + listener.BoundPort.ToString(CultureInfo.InvariantCulture) +
                " keyed=" + (sessionKey.Length > 0 ? "true" : "false") +
                " playing-client=true");
        }

        private void AbortHostingStart(string reason)
        {
            DisposeTransport();
            machine = null;
            StopTestDrivers();
            session?.Shutdown();
            session = null;
            console = null;
            listener?.Dispose();
            listener = null;
            presenter.ShowEntry(LocalSeats(), reason, ComposedFormatLabel());
        }

        /// <summary>Optional test pacing override; normal bots use the existing two-second pace.</summary>
        internal const string ThinkSecondsOverrideEnvironmentVariable = "GUARDS_TESTDRIVER_THINK_SECONDS";

        /// <summary>Fill only open seats through the in-process encoded protocol; repeated presses cannot stack bots.</summary>
        private void FillEmptySeatsWithTestDrivers()
        {
            if (botFleet == null) return;
            var thinkSeconds = PrivateSeatDriverOptions.DefaultThinkSeconds;
            var overrideText = Environment.GetEnvironmentVariable(ThinkSecondsOverrideEnvironmentVariable);
            if (double.TryParse(overrideText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var parsed) && !double.IsInfinity(parsed) && parsed >= 0 && parsed <= 60)
                thinkSeconds = parsed;
            var added = botFleet.FillOpenSeats(DateTime.UtcNow.Ticks, thinkSeconds);
            testDriverSeats.Clear();
            testDriverSeats.UnionWith(botFleet.Seats);
            presenter.SetTestDriverSeats(testDriverSeats, added == 0
                ? "Every open seat already has a bot joining."
                : "Connected " + added + " in-process bot(s). Ready your human seat to begin picking.");
            PrivateOnlineMarkers.Log("LOBBY_BOTS_CONNECTED count=" + added);
        }

        private void StopTestDrivers()
        {
            botFleet?.Dispose();
            botFleet = null;
            reportedBotFailure = string.Empty;
            testDriverSeats.Clear();
        }

        private void BeginJoining(PrivateLobbyEntry entry)
        {
            // 8.35t corrective. Cleared on every attempt, not only a failing one: a retry that
            // succeeds must not leave a stale failure standing for a caller - a spawned
            // PrivateSeatDriver among them - that only checks this once, right after pressing.
            LastJoinFailure = null;
            // PLAYTEST FAILURE #2 fix. A fresh join gets one fresh format-inheritance attempt, and
            // no stale pick refusal carried over from a prior seating.
            formatInheritanceAttempted = false;
            lastSurfacedLobbyRefusal = string.Empty;

            if (!TryReadEndpoint(entry, out var endpoint, out var error))
            {
                LastJoinFailure = error;
                PrivateOnlineMarkers.Fail("lobby-join " + error);
                presenter.ShowEntry(LocalSeats(), error);
                return;
            }

            joinedSeat = entry.Seat;
            sessionKey = entry.SessionKey;
            dialled = endpoint;
            refusalReported = false;
            // Milestone 8.35k. The road back for a relaunched process. The store is consulted by
            // the client itself once the host names the session in its Welcome - the correlation is
            // the host's to assign, so it cannot be keyed here, before the handshake.
            machine = new PrivateSessionClient(
                compositionRoot.Runtime.SessionIdentity, joinedSeat, sessionKey,
                clientOutbox, new CryptoNonceSource(),
                retainedCapabilities: RetainedCapabilities());
            OpenTransport();
            if (!Dial())
            {
                // 8.35t corrective. Dial() already wrote its own reason onto the presenter's
                // feedback line; before this fix that was the ONLY place it went - invisible to
                // any log reader and to a spawned driver, which has no feedback line of its own
                // anyone is watching. A named seat makes this readable out of context.
                LastJoinFailure = "seat=" + SeatLabelForLog() +
                    " could not reach the host at " + dialled;
                PrivateOnlineMarkers.Fail("lobby-join " + LastJoinFailure);
                machine = null;
                return;
            }

            signature = string.Empty;
            presenter.AttachClient(machine);
            PrivateOnlineMarkers.Log("LOBBY_JOINING seat=" + SeatLabelForLog());
        }

        /// <summary>
        /// PLAYTEST FAILURE #2 fix (lobby recut). The format-inheritance recovery: a joiner cannot
        /// know the host's chosen team size and game length before it dials - it types only an
        /// address - so it composes <see cref="MatchFormat.Default"/> (3v3 Quick) at Awake and derives
        /// its admission tuple from that. A host playing anything else refuses it
        /// <see cref="SessionRejectionReason.MatchFormatMismatch"/>, which is the exact flood in the
        /// returned 2v2 host.log and the reason every 2v2 join was silently impossible in the shipped
        /// build. The user's own spec correction is that a joiner INHERITS the host's format, and the
        /// host's refusal already NAMES its format id in the reject detail, so this reads it,
        /// recomposes this scene onto that format - which rebuilds the runtime and, with it, the
        /// admission tuple <see cref="ConfiguredMatchSceneRuntime.SessionIdentity"/> derives from the
        /// composition - and re-dials once with the corrected identity.
        /// </summary>
        /// <remarks>
        /// A host never runs this: it composed the format itself and refuses nobody over it. It runs
        /// at most once per join (<see cref="formatInheritanceAttempted"/>), so a SECOND format
        /// refusal - a genuinely incompatible board, not the default-versus-host case - is left to
        /// fail by name rather than looped. No wire change: the host has published its own format id
        /// in the plaintext refusal since the ELEVENTH amendment, and reading it is exactly the
        /// "told the fact it can act on" the reason enum's own comment intends.
        /// </remarks>
        private bool TryInheritHostFormatAndRejoin()
        {
            if (session != null || machine == null || formatInheritanceAttempted ||
                compositionRoot?.Runtime == null)
            {
                return false;
            }

            if (machine.State != PrivateSessionClientState.Rejected ||
                machine.RejectionReason != SessionRejectionReason.MatchFormatMismatch)
            {
                return false;
            }

            var hostFormat = HostFormatFromRefusal(machine.RejectionDetail);
            if (hostFormat == null)
            {
                // The refusal named no printed id this build composes - a custom staged board, say -
                // so there is nothing to inherit and the honest answer is the refusal that stands.
                return false;
            }

            formatInheritanceAttempted = true;

            if (compositionRoot.ComposedPlan == null ||
                !string.Equals(
                    compositionRoot.ComposedPlan.Format.Id, hostFormat.Id, StringComparison.Ordinal))
            {
                compositionRoot.Recompose(MatchCompositionPlan.Printed(hostFormat));
                PrivateOnlineMarkers.Log("LOBBY_FORMAT_INHERITED format=" + hostFormat.Id);
            }

            // The seat a player pre-selected on the entry form may not exist in the host's format - a
            // joiner who typed red-player-03 against a 2v2 host names a seat 2v2 does not have. Keep it
            // only when the recomposed match actually seats it; otherwise inherit seatless, so the
            // joiner is admitted with the roster and picks one of the seats the host's format defines,
            // which is the spec flow ("see open seats -> pick a seat") reached the honest way.
            if (joinedSeat.IsDefined && !SeatExistsInComposedMatch(joinedSeat))
            {
                joinedSeat = default;
            }

            // A fresh client, from the NOW-corrected admission tuple. The refused one is terminal and
            // holds the wrong identity; nothing of it is reused because no seat capability has been
            // minted yet.
            DisposeTransport();
            refusalReported = false;
            machine = new PrivateSessionClient(
                compositionRoot.Runtime.SessionIdentity, joinedSeat, sessionKey,
                clientOutbox, new CryptoNonceSource(),
                retainedCapabilities: RetainedCapabilities());
            OpenTransport();
            if (!Dial())
            {
                LastJoinFailure = "seat=" + SeatLabelForLog() +
                    " could not re-reach the host at " + dialled +
                    " after inheriting format " + hostFormat.Id;
                PrivateOnlineMarkers.Fail("lobby-join " + LastJoinFailure);
                machine = null;
                return true;
            }

            signature = string.Empty;
            presenter.AttachClient(machine);
            PrivateOnlineMarkers.Log(
                "LOBBY_REJOINING seat=" + SeatLabelForLog() + " format=" + hostFormat.Id);
            return true;
        }

        /// <summary>
        /// The printed format a <see cref="SessionRejectionReason.MatchFormatMismatch"/> refusal
        /// names, or null when it names none this build composes. The host's own
        /// <c>DescribeMismatch</c> writes the stable <see cref="MatchFormat.Id"/> into the plaintext
        /// detail; this scans for whichever printed id appears rather than parsing the sentence, so a
        /// reworded message can never silently defeat inheritance.
        /// </summary>
        private static MatchFormat HostFormatFromRefusal(string detail)
        {
            if (string.IsNullOrEmpty(detail))
            {
                return null;
            }

            foreach (var format in MatchFormat.All)
            {
                if (detail.IndexOf(format.Id, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return format;
                }
            }

            return null;
        }

        /// <summary>Whether the currently composed match seats a seat of that id.</summary>
        private bool SeatExistsInComposedMatch(SeatId seat)
        {
            foreach (var composed in compositionRoot.Runtime.Host.Seats.Seats)
            {
                if (composed.Id.Equals(seat))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// PLAYTEST FAILURE #2 fix (lobby recut). Writes a REFUSED hero pick to the log once per
        /// distinct refusal. A pick the host declines - out of turn, a hero taken a moment earlier,
        /// the phase not open - is a NON-TERMINAL refusal: the client records the reason and stays
        /// Seated (a refused pick must not unseat anyone), so the terminal
        /// <c>LOBBY_REFUSED</c> path never fired for it and the returned playtest log carried no
        /// trace at all. That silence is the diagnosis's own answer to "why did this ship twice", and
        /// this closes it without a visible surface that could go stale after a later pick succeeds.
        /// </summary>
        private void SurfaceLobbyPickRefusal()
        {
            if (machine == null || machine.HasMatchStarted ||
                machine.State != PrivateSessionClientState.Seated ||
                machine.RejectionReason != SessionRejectionReason.HeroSelectionRefused)
            {
                return;
            }

            if (string.Equals(lastSurfacedLobbyRefusal, machine.RejectionDetail, StringComparison.Ordinal))
            {
                return;
            }

            lastSurfacedLobbyRefusal = machine.RejectionDetail;
            PrivateOnlineMarkers.Log(
                "LOBBY_PICK_REFUSED seat=" + SeatLabelForLog() +
                " reason=" + machine.RejectionReason +
                " detail=" + machine.RejectionDetail);
        }

        /// <summary>
        /// 8.35z54. The seat a log line names. A player who has joined the server but not
        /// yet taken a chair genuinely has none, and saying so is better than printing an empty
        /// value into a returned playtest log - which is the diagnostic gap milestone 8.35k paid a
        /// day for.
        /// </summary>
        private string SeatLabelForLog() =>
            joinedSeat.IsDefined ? joinedSeat.Value : "(unseated)";

        /// <summary>
        /// Dials the host that was typed, for a first join or for an armed return. A refused or
        /// unreachable host is words on the entry form, not a silent state - the join-failure half
        /// of the block's second acceptance check.
        /// </summary>
        private bool Dial()
        {
            if (transport.Connect(dialled, ConnectBudgetSeconds, out var error))
            {
                return true;
            }

            DisposeTransport();
            presenter.ShowEntry(LocalSeats(), "The host could not be reached: " + error);
            return false;
        }

        /// <summary>
        /// Dials again for a return the presenter has already armed. The machine is the same one -
        /// it holds the capability the host will ask for, and a fresh client would hold none - so
        /// only the socket under its outbox is replaced.
        /// </summary>
        private void Redial()
        {
            if (machine == null)
            {
                return;
            }

            DisposeTransport();
            OpenTransport();
            if (!Dial())
            {
                machine.OnReconnectTransportFailed();
                signature = string.Empty;
                presenter.AttachClient(machine);
                return;
            }

            signature = string.Empty;
            presenter.AttachClient(machine);
            PrivateOnlineMarkers.Log("LOBBY_RECONNECTING seat=" + joinedSeat.Value);
        }

        /// <summary>
        /// Closes this client's real socket for the opt-in rehearsal, then reports the observed
        /// loss through the same client state machine the transport would notify. Reconnect still
        /// has to be pressed through <see cref="PrivateLobbyPresenter"/> and therefore retains the
        /// same capability-bearing machine while replacing only its transport.
        /// </summary>
        internal bool InterruptConnectionForRehearsal()
        {
            if (machine == null || machine.State != PrivateSessionClientState.Seated)
            {
                return false;
            }

            DisposeTransport();
            machine.OnDisconnected();
            signature = string.Empty;
            presenter?.Refresh();
            return true;
        }

        private void OpenTransport()
        {
            transport = new TcpSessionClientTransport();
            clientOutbox.Live = transport;
        }

        private void Leave()
        {
            CloseEvidence();
            var departing = machine;
            DisposeTransport();
            departing?.OnDisconnected();
            machine = null;
            StopTestDrivers();
            if (session != null)
            {
                session.Shutdown();
                session = null;
            }

            console = null;
            listener?.Dispose();
            listener = null;
            onlineHudAttached = false;
            outcomeReported = false;
            if (matchStatus != null)
            {
                matchStatus.text = string.Empty;
                matchStatus.style.display = DisplayStyle.None;
            }

            compositionRoot?.Runtime?.RefreshAll();
            signature = string.Empty;
            presenter.ShowEntry(LocalSeats(), string.Empty);
            PrivateOnlineMarkers.Log("LOBBY_LEFT");
        }

        private static bool TryReadEndpoint(
            PrivateLobbyEntry entry, out PrivateSessionEndpoint endpoint, out string error)
        {
            endpoint = null;
            if (entry == null)
            {
                error = "An address and port are required.";
                return false;
            }

            if (!int.TryParse(entry.Port, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var port))
            {
                error = "A port is a whole number between 1 and 65535.";
                return false;
            }

            return PrivateSessionEndpoint.TryCreate(entry.Address, port, out endpoint, out error);
        }

        private void LateUpdate()
        {
            if (panelSettings == null || presenter?.Model == null) return;
            // The menu can scroll, so short windows must not shrink it to the game's full-board
            // footprint. Restore the shared HUD coordinates for in-match connection notices.
            var preMatch = !presenter.Model.HasMatchStarted;
            panelSettings.screenMatchMode = preMatch ? PanelScreenMatchMode.MatchWidthOrHeight : MatchHudPanelScaling.ScreenMatchMode;
            panelSettings.match = preMatch ? 0f : MatchHudPanelScaling.Match;
        }

        private void BuildPanel()
        {
            document = gameObject.GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            // Milestone 8.25c's coordinate space, reused rather than reinvented: the lobby's sizes
            // are reference units in exactly the space the HUD's are, so the same controls are
            // reachable at every supported desktop viewport.
            var authored = Resources.Load<PanelSettings>(
                ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath);
            panelSettings = authored != null
                ? Instantiate(authored)
                : ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Guards Private Lobby Panel";
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            MatchHudPanelScaling.Apply(panelSettings);
            panelSettings.clearColor = false;
            panelSettings.sortingOrder = LobbySortingOrder;
            var theme = Resources.Load<ThemeStyleSheet>(
                ConfiguredMatchHudRenderer.RuntimeThemeResourcePath);
            if (theme != null)
            {
                panelSettings.themeStyleSheet = theme;
            }

            document.panelSettings = panelSettings;
            if (document.rootVisualElement == null)
            {
                document.enabled = false;
                document.enabled = true;
            }

            var root = document.rootVisualElement;
            if (root != null)
            {
                root.name = "gd-lobby-document";
                root.pickingMode = PickingMode.Ignore;
            }
        }

        private void DisposeTransport()
        {
            transport?.Dispose();
            transport = null;
            clientOutbox.Live = null;
        }

        private void OnDestroy()
        {
            CloseEvidence();
            DisposeTransport();
            StopTestDrivers();
            session?.Shutdown();
            session = null;
            listener?.Dispose();
            listener = null;
            onlineHudAttached = false;
            if (panelSettings != null)
            {
                Destroy(panelSettings);
                panelSettings = null;
            }
        }
    }
}
