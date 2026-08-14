using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionUnityView : IBoardInteractionView
    {
        private readonly IReadOnlyList<PrimitiveHeroView> heroes;
        private IReadOnlyList<StandardMinionView> minions;
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly Material highlightMaterial;
        private Func<HeroId, bool> heroIntentHandler;
        private Func<MinionId, bool> minionIntentHandler;
        private Func<GuardsDigital.Core.Board.HexCoordinate, bool> destinationIntentHandler;

        public BoardInteractionUnityView(
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<HexTileView> tiles,
            Material highlightMaterial,
            IEnumerable<StandardMinionView> minions = null)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.minions = Array.AsReadOnly((minions ?? Enumerable.Empty<StandardMinionView>())
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            this.highlightMaterial = highlightMaterial ?? throw new ArgumentNullException(nameof(highlightMaterial));
        }

        public BoardInteractionViewModel Model { get; private set; }
        public IReadOnlyList<StandardMinionView> MinionViews => minions;

        public event Action<HeroId> HeroSubmitted;
        public event Action<MinionId> MinionSubmitted;
        public event Action<GuardsDigital.Core.Board.HexCoordinate> DestinationSubmitted;

        public void BindIntentHandlers(
            Func<HeroId, bool> submitHero,
            Func<MinionId, bool> submitMinion,
            Func<GuardsDigital.Core.Board.HexCoordinate, bool> submitDestination)
        {
            heroIntentHandler = submitHero ?? throw new ArgumentNullException(nameof(submitHero));
            minionIntentHandler = submitMinion ?? throw new ArgumentNullException(nameof(submitMinion));
            destinationIntentHandler = submitDestination ??
                throw new ArgumentNullException(nameof(submitDestination));
        }

        public void Render(BoardInteractionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            foreach (var hero in heroes)
            {
                var occupancy = model.Heroes.SingleOrDefault(value =>
                    value.HeroId.Equals(hero.Occupancy.HeroId));
                if (occupancy.HeroId.IsDefined)
                {
                    hero.Configure(occupancy, hero.OuterRadius);
                }
            }

            RenderMinions(model);

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(model.MovementDestinations);
            targetCoordinates.UnionWith(model.FastTravelDestinations);
            foreach (var targetId in model.AttackTargets)
            {
                var target = model.Heroes.Single(value => value.HeroId.Equals(targetId));
                targetCoordinates.Add(target.Coordinate);
            }
            foreach (var targetId in model.MinionAttackTargets)
            {
                var target = model.Minions.Single(value => value.MinionId.Equals(targetId));
                targetCoordinates.Add(target.Coordinate);
            }

            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                var highlighted = targetCoordinates.Contains(tile.Coordinate);
                tile.SetHighlighted(highlighted, highlighted ? highlightMaterial : null);
            }
        }

        public void ReplaceMinionViews(IEnumerable<StandardMinionView> currentMinions)
        {
            minions = Array.AsReadOnly((currentMinions ??
                    throw new ArgumentNullException(nameof(currentMinions)))
                .Where(value => value != null)
                .Distinct()
                .ToArray());
            if (Model != null)
            {
                RenderMinions(Model);
            }
        }

        private void RenderMinions(BoardInteractionViewModel model)
        {
            var projectedMinions = model.Minions.ToDictionary(value => value.MinionId);
            var defenseCues = model.MinionDefenseCues.ToDictionary(value => value.MinionId, value => value.Amount);
            var choiceCues = model.MinionChoiceCues.ToDictionary(value => value.MinionId);
            foreach (var minion in minions)
            {
                var boundId = minion.BoundMinionId;
                if (boundId.IsDefined && projectedMinions.TryGetValue(boundId, out var state))
                {
                    minion.gameObject.SetActive(true);
                    if (!ReferenceEquals(minion.Occupancy, state))
                    {
                        minion.Configure(state, minion.OuterRadius);
                    }

                    minion.SetDefenseContributionCue(
                        defenseCues.TryGetValue(boundId, out var amount) ? amount : 0);
                    minion.SetChoiceCue(choiceCues.TryGetValue(boundId, out var choiceCue)
                        ? choiceCue.Kind
                        : MinionChoiceCueKind.None);

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.SetDefenseContributionCue(0);
                    minion.SetChoiceCue(MinionChoiceCueKind.None);
                    minion.gameObject.SetActive(false);
                }
            }

            if (projectedMinions.Count > 0)
            {
                throw new InvalidOperationException("The configured board has no view for a projected minion identity.");
            }
        }

        public bool TryForwardHeroIntent(HeroId heroId)
        {
            if (heroIntentHandler != null)
            {
                return heroIntentHandler(heroId);
            }

            if (HeroSubmitted == null)
            {
                return false;
            }

            HeroSubmitted.Invoke(heroId);
            return true;
        }

        public bool TryForwardMinionIntent(MinionId minionId)
        {
            if (minionIntentHandler != null)
            {
                return minionIntentHandler(minionId);
            }

            if (MinionSubmitted == null)
            {
                return false;
            }

            MinionSubmitted.Invoke(minionId);
            return true;
        }

        public bool TryForwardDestinationIntent(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            if (destinationIntentHandler != null)
            {
                return destinationIntentHandler(destination);
            }

            if (DestinationSubmitted == null)
            {
                return false;
            }

            DestinationSubmitted.Invoke(destination);
            return true;
        }
    }

    public sealed class DirectorHandsUnityView : IDirectorHandsView
    {
        public DirectorHandsViewModel Model { get; private set; }

        public event Action<CardInstanceId> CardSelected;
        public event Action ConfirmRequested;
        public event Action<HeroId> PassRequested;

        public void Render(DirectorHandsViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            foreach (var hand in Model.Hands)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(hand.HeaderLabel, GUILayout.Width(122f));
                GUILayout.Label(hand.CommitmentLabel);
                if (hand.CanPass && GUILayout.Button("PASS - NO CARD", GUILayout.Width(125f), GUILayout.Height(24f)))
                {
                    PassRequested?.Invoke(hand.HeroId);
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                foreach (var card in hand.Cards)
                {
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = hand.SelectedCardInstanceId.HasValue &&
                        hand.SelectedCardInstanceId.Value.Equals(card.Id)
                            ? new Color(0.35f, 0.75f, 1f)
                            : Color.white;
                    if (GUILayout.Button(
                        hand.CardButtonLabels[card.Id],
                        GUILayout.MinWidth(112f),
                        GUILayout.Height(46f)))
                    {
                        CardSelected?.Invoke(card.Id);
                    }

                    GUI.backgroundColor = previous;
                }

                GUILayout.EndHorizontal();
            }

            var hasSelection = false;
            for (var index = 0; index < Model.Hands.Count; index++)
            {
                if (Model.Hands[index].SelectedCardInstanceId.HasValue)
                {
                    hasSelection = true;
                    break;
                }
            }

            if (hasSelection)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                if (GUILayout.Button("CONFIRM FACEDOWN", GUILayout.Width(170f), GUILayout.Height(38f)))
                {
                    ConfirmRequested?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
        }

    }

    public sealed class RoundActionUnityView : IRoundActionView
    {
        private const double PreviewDismissDelaySeconds = 0.12d;
        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);
        private DefenseBreakdownViewModel activeDefensePreview;
        private Rect defensePreviewAnchor;
        private double dismissPreviewAt = -1d;

        public RoundActionViewModel Model { get; private set; }
        public DefenseBreakdownViewModel ActiveDefensePreview => activeDefensePreview;
        public Rect DefensePreviewAnchor => defensePreviewAnchor;

        public event Action<TypedInvocationViewModel> InvocationSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;

        public void Render(RoundActionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            if (activeDefensePreview != null &&
                (activeDefensePreview.Version != Model.Version ||
                 !Model.DefenseChoices.Any(value =>
                     ReferenceEquals(value.DefenseBreakdown, activeDefensePreview))))
            {
                ClearDefensePreview();
            }
        }

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            GUILayout.Label(Model.Status);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Actions", GUILayout.Width(54f));
            DrawInvocation(Model.ContinueEndOfRoundInvocation, 200f);
            foreach (var choice in Model.InitiativeChoices)
            {
                DrawInvocation(choice, 150f);
            }

            foreach (var action in Model.Actions)
            {
                DrawInvocation(action);
            }

            foreach (var fastTravel in Model.FastTravelOffers)
            {
                if (GUILayout.Button(fastTravel.Label, GUILayout.Height(46f)))
                {
                    ForwardFastTravelSelection(fastTravel);
                }
            }

            // The decision a paused effect frame waits on. It sits in the same row as the actions it
            // replaces, because the two are never offered at once.
            foreach (var effectChoice in Model.EffectChoices)
            {
                DrawInvocation(effectChoice, 170f);
            }

            GUILayout.FlexibleSpace();
            DrawInvocation(Model.EndTurnInvocation, 130f);
            GUILayout.EndHorizontal();

            if (Model.DefenseChoices.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Defense", GUILayout.Width(54f));
                var previewActiveThisEvent = false;
                for (var index = 0; index < Model.DefenseChoices.Count; index++)
                {
                    previewActiveThisEvent |= DrawContentSizedDefenseInvocation(
                        Model.DefenseChoices[index],
                        index);
                }

                GUILayout.EndHorizontal();
                if (Event.current.type == EventType.Repaint && !previewActiveThisEvent)
                {
                    ScheduleDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
                }
            }
            else
            {
                ClearDefensePreview();
            }

            UpdateDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
        }

        public void ForwardInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                InvocationSubmitted?.Invoke(invocation);
            }
        }

        public void ForwardFastTravelSelection(FastTravelOfferViewModel selection)
        {
            if (selection != null)
            {
                FastTravelSelected?.Invoke(selection);
            }
        }

        private void DrawInvocation(TypedInvocationViewModel invocation, float width)
        {
            if (invocation != null && GUILayout.Button(invocation.Label, GUILayout.Width(width), GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
        }

        private void DrawInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation != null && GUILayout.Button(invocation.Label, GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
        }

        private bool DrawContentSizedDefenseInvocation(TypedInvocationViewModel invocation, int index)
        {
            if (invocation == null)
            {
                return false;
            }

            var minimumWidth = CalculateContentWidth(invocation.Label, GUI.skin.button);
            var controlName = $"DefenseChoice-{Model.Version}-{index}";
            GUI.SetNextControlName(controlName);
            if (GUILayout.Button(
                invocation.Label,
                GUILayout.MinWidth(minimumWidth),
                GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }

            if (Event.current.type != EventType.Repaint || invocation.DefenseBreakdown == null)
            {
                return false;
            }

            var localRect = GUILayoutUtility.GetLastRect();
            var screenTopLeft = GUIUtility.GUIToScreenPoint(localRect.position);
            var screenRect = new Rect(screenTopLeft, localRect.size);
            var active = localRect.Contains(Event.current.mousePosition) ||
                string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal);
            if (active)
            {
                ShowDefensePreview(invocation.DefenseBreakdown, screenRect);
            }

            return active;
        }

        public void ShowDefensePreview(DefenseBreakdownViewModel preview, Rect anchorScreenRect)
        {
            activeDefensePreview = preview ?? throw new ArgumentNullException(nameof(preview));
            defensePreviewAnchor = anchorScreenRect;
            dismissPreviewAt = -1d;
        }

        public void ScheduleDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt < 0d)
            {
                dismissPreviewAt = now + PreviewDismissDelaySeconds;
            }
        }

        public void UpdateDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt >= 0d && now >= dismissPreviewAt)
            {
                ClearDefensePreview();
            }
        }

        public void DrawDefensePreviewOverlay()
        {
            if (activeDefensePreview == null)
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontSize = 12,
                richText = false
            };
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
            var lines = activeDefensePreview.Text.Split('\n');
            var columnCount = lines.Length > 12 ? 2 : 1;
            var rowsPerColumn = Mathf.CeilToInt(lines.Length / (float)columnCount);
            var columns = Enumerable.Range(0, columnCount)
                .Select(column => string.Join("\n", lines
                    .Skip(column * rowsPerColumn)
                    .Take(rowsPerColumn)))
                .ToArray();
            var width = columnCount == 1 ? 440f : 680f;
            var columnWidth = (width - 24f) / columnCount;
            var textHeight = columns.Max(column => style.CalcHeight(
                new GUIContent(column),
                columnWidth));
            var preferred = new Vector2(width, textHeight + 44f);
            var rect = CalculateDefensePreviewRect(
                defensePreviewAnchor,
                new Vector2(Screen.width, Screen.height),
                preferred);
            var previousBackgroundColor = GUI.color;
            GUI.color = DefensePreviewBackgroundColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousBackgroundColor;
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f),
                activeDefensePreview.Title, titleStyle);
            var body = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, rect.height - 40f);
            var renderedColumnWidth = body.width / columnCount;
            for (var column = 0; column < columns.Length; column++)
            {
                GUI.Label(new Rect(
                    body.x + (column * renderedColumnWidth),
                    body.y,
                    renderedColumnWidth,
                    body.height),
                    columns[column],
                    style);
            }
        }

        public static Rect CalculateDefensePreviewRect(
            Rect anchor,
            Vector2 viewport,
            Vector2 preferredSize)
        {
            const float margin = 8f;
            var width = Mathf.Min(preferredSize.x, Mathf.Max(0f, viewport.x - (margin * 2f)));
            var height = Mathf.Min(preferredSize.y, Mathf.Max(0f, viewport.y - (margin * 2f)));
            var above = anchor.y - margin;
            var below = viewport.y - anchor.yMax - margin;
            var right = viewport.x - anchor.xMax - margin;
            var left = anchor.x - margin;

            if (above >= height)
            {
                return new Rect(
                    Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                    anchor.y - height - margin,
                    width,
                    height);
            }

            if (right >= width)
            {
                return new Rect(anchor.xMax + margin,
                    Mathf.Clamp(anchor.center.y - (height * 0.5f), margin, viewport.y - height - margin),
                    width, height);
            }

            if (left >= width)
            {
                return new Rect(anchor.x - width - margin,
                    Mathf.Clamp(anchor.center.y - (height * 0.5f), margin, viewport.y - height - margin),
                    width, height);
            }

            if (below >= height)
            {
                return new Rect(
                    Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                    anchor.yMax + margin,
                    width,
                    height);
            }

            var verticalSpace = Mathf.Max(above, below);
            var boundedHeight = Mathf.Min(height, verticalSpace);
            var y = above >= below ? anchor.y - boundedHeight - margin : anchor.yMax + margin;
            return new Rect(
                Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                y,
                width,
                boundedHeight);
        }

        private void ClearDefensePreview()
        {
            activeDefensePreview = null;
            dismissPreviewAt = -1d;
        }

        public static float CalculateContentWidth(string label, GUIStyle style)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            return Mathf.Ceil(style.CalcSize(new GUIContent(label)).x);
        }
    }

    /// <summary>
    /// The configured contextual prompt for a pending minion choice. It draws one control per
    /// candidate the authority currently offers a legal invocation for, reports which control the
    /// pointer or keyboard focus is on so the board can mark that minion, and forwards the projected
    /// typed invocation on click. It holds no session and decides no legality.
    /// </summary>
    public sealed class MinionChoiceUnityView : IMinionChoiceView
    {
        private MinionId highlightedMinionId;

        public MinionChoiceViewModel Model { get; private set; }

        /// <summary>The candidate the pointer or keyboard focus is currently on, if any.</summary>
        public MinionId HighlightedMinionId => highlightedMinionId;

        public event Action<MinionChoiceCandidateViewModel> CandidateSubmitted;
        public event Action ConfirmRequested;
        public event Action<MinionId> CandidateHighlighted;

        public void Render(MinionChoiceViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            if (Model == null)
            {
                SetHighlight(default);
            }
        }

        public void Draw()
        {
            // The model is read once: forwarding an invocation below refreshes the presenters inside
            // this same draw, and the loop must keep walking the controls it started with.
            var model = Model;
            if (model == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Minion Battle", GUILayout.Width(84f));
            GUILayout.Label($"{model.PromptLabel}   {model.StatusLabel}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(84f);
            var hovered = default(MinionId);
            for (var index = 0; index < model.Candidates.Count; index++)
            {
                if (DrawCandidate(model, index))
                {
                    hovered = model.Candidates[index].MinionId;
                }
            }

            if (model.ConfirmInvocation != null && GUILayout.Button(
                model.ConfirmInvocation.Label,
                GUILayout.MinWidth(RoundActionUnityView.CalculateContentWidth(
                    model.ConfirmInvocation.Label,
                    GUI.skin.button)),
                GUILayout.Height(46f)))
            {
                ConfirmRequested?.Invoke();
            }

            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                SetHighlight(hovered);
            }
        }

        public void ForwardCandidate(MinionChoiceCandidateViewModel candidate)
        {
            if (candidate != null)
            {
                CandidateSubmitted?.Invoke(candidate);
            }
        }

        public void ForwardConfirm()
        {
            ConfirmRequested?.Invoke();
        }

        public void ForwardHighlight(MinionId minionId)
        {
            SetHighlight(minionId);
        }

        private bool DrawCandidate(MinionChoiceViewModel model, int index)
        {
            var candidate = model.Candidates[index];
            var controlName = $"MinionChoice-{model.Version}-{index}";
            GUI.SetNextControlName(controlName);
            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = candidate.IsSelected
                ? new Color(0.95f, 0.62f, 0.25f)
                : previousBackground;
            var pressed = GUILayout.Button(
                candidate.Label,
                GUILayout.MinWidth(RoundActionUnityView.CalculateContentWidth(candidate.Label, GUI.skin.button)),
                GUILayout.Height(46f));
            GUI.backgroundColor = previousBackground;
            if (pressed)
            {
                ForwardCandidate(candidate);
            }

            if (Event.current.type != EventType.Repaint)
            {
                return false;
            }

            var rect = GUILayoutUtility.GetLastRect();
            return rect.Contains(Event.current.mousePosition) ||
                string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal);
        }

        private void SetHighlight(MinionId minionId)
        {
            if (highlightedMinionId.Equals(minionId))
            {
                return;
            }

            highlightedMinionId = minionId;
            CandidateHighlighted?.Invoke(minionId);
        }
    }

    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;
        public event Action RestartRequested;

        public void Render(UndoRestartViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            GUI.enabled = Model.CanUndo;
            if (GUILayout.Button("UNDO", GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                UndoRequested?.Invoke();
            }

            GUI.enabled = Model.CanRestart;
            if (GUILayout.Button("RESTART", GUILayout.Width(90f), GUILayout.Height(24f)))
            {
                RestartRequested?.Invoke();
            }

            GUI.enabled = true;
        }
    }

    public sealed class DiagnosticLogUnityView : IDiagnosticLogView
    {
        private Vector2 scrollPosition;

        public DiagnosticLogViewModel Model { get; private set; }

        public event Action ToggleRequested;

        public void Render(DiagnosticLogViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void DrawToggle()
        {
            if (Model != null && GUILayout.Button(
                Model.ToggleLabel,
                GUILayout.Width(126f),
                GUILayout.Height(24f)))
            {
                ToggleRequested?.Invoke();
            }
        }

        public void DrawPanel(Rect panel)
        {
            if (Model == null || !Model.IsExpanded)
            {
                return;
            }

            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));
            GUILayout.Label(Model.PanelLabel);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var entry in Model.Entries)
            {
                GUILayout.Label(entry.DisplayText);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
