using System;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class MatchHudHeaderViewModel
    {
        public MatchHudHeaderViewModel(TieBreakerState tieBreaker, QuickGameSetupState setup)
        {
            TieBreaker = tieBreaker;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Text = Format(tieBreaker, setup);
            Color = tieBreaker.IsDefined
                ? tieBreaker.RevealedSide == TeamSide.Red
                    ? new Color(0.72f, 0.16f, 0.12f)
                    : new Color(0.12f, 0.32f, 0.72f)
                : new Color(0.3f, 0.3f, 0.3f);
        }

        public TieBreakerState TieBreaker { get; }
        public QuickGameSetupState Setup { get; }
        public string Text { get; }
        public Color Color { get; }

        private static string Format(TieBreakerState state, QuickGameSetupState setup)
        {
            var redLives = setup.Teams.Single(value => value.TeamSide == TeamSide.Red).LifeCount;
            var blueLives = setup.Teams.Single(value => value.TeamSide == TeamSide.Blue).LifeCount;
            var tieBreaker = !state.IsDefined
                ? "TIE BREAKER  [?] UNKNOWN"
                : state.RevealedSide == TeamSide.Red
                    ? "TIE BREAKER  [R] RED"
                    : "TIE BREAKER  [B] BLUE";
            return $"QUICK GAME  WAVES {setup.WaveCount}\nLIVES  [R] {redLives}  [B] {blueLives}\n{tieBreaker}";
        }
    }

    [DisallowMultipleComponent]
    public sealed class ConfiguredMatchHudRenderer : MonoBehaviour
    {
        private const float Margin = 12f;
        private const float CardPanelWidth = 1280f;
        private const float CardPanelHeight = 360f;
        private const float TieBreakerWidth = 200f;
        private const float TieBreakerHeight = 96f;
        private const float EventLogPreferredWidth = 720f;
        private const float EventLogPreferredHeight = 360f;

        private DirectorHandsUnityView hands;
        private RoundActionUnityView round;
        private UndoRestartUnityView undoRestart;
        private DiagnosticLogUnityView diagnostics;
        private MinionChoiceUnityView minionChoice;
        private MatchHudHeaderViewModel header;
        private GUIStyle tieBreakerStyle;
        private Vector2 cardPanelScrollPosition;

        public MatchHudHeaderViewModel Header => header;
        public Rect CardPanelRect => GetCardPanelRect();
        public Rect TieBreakerIndicatorRect => GetTieBreakerIndicatorRect();
        public Rect RuleEventLogPanelRect => GetRuleEventLogRect();

        public void Configure(
            DirectorHandsUnityView handsView,
            RoundActionUnityView roundView,
            UndoRestartUnityView undoRestartView,
            DiagnosticLogUnityView diagnosticView,
            MinionChoiceUnityView minionChoiceView)
        {
            hands = handsView ?? throw new ArgumentNullException(nameof(handsView));
            round = roundView ?? throw new ArgumentNullException(nameof(roundView));
            undoRestart = undoRestartView ?? throw new ArgumentNullException(nameof(undoRestartView));
            diagnostics = diagnosticView ?? throw new ArgumentNullException(nameof(diagnosticView));
            minionChoice = minionChoiceView ?? throw new ArgumentNullException(nameof(minionChoiceView));
        }

        public void RenderHeader(MatchHudHeaderViewModel model)
        {
            header = model ?? throw new ArgumentNullException(nameof(model));
        }

        public bool BlocksPointerAt(Vector2 screenPosition)
        {
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return GetCardPanelRect().Contains(guiPosition) ||
                GetTieBreakerIndicatorRect().Contains(guiPosition) ||
                diagnostics?.Model?.IsExpanded == true && GetRuleEventLogRect().Contains(guiPosition);
        }

        private void OnGUI()
        {
            // The minion-choice view is checked for presence only: its model is null whenever no
            // minion choice is pending, which is the ordinary state and must not blank the HUD.
            if (header == null || hands?.Model == null || round?.Model == null ||
                undoRestart?.Model == null || diagnostics?.Model == null || minionChoice == null)
            {
                return;
            }

            DrawTieBreaker();
            var panel = GetCardPanelRect();
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(round.Model.HudHeader);
            undoRestart.Draw();
            diagnostics.DrawToggle();
            GUILayout.EndHorizontal();
            cardPanelScrollPosition = GUILayout.BeginScrollView(cardPanelScrollPosition);
            hands.Draw();
            round.Draw();
            minionChoice.Draw();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            round.DrawDefensePreviewOverlay();
            diagnostics.DrawPanel(GetRuleEventLogRect());
        }

        private void DrawTieBreaker()
        {
            if (!header.TieBreaker.IsDefined)
            {
                return;
            }

            var indicator = GetTieBreakerIndicatorRect();
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            GUI.color = header.Color;
            GUI.DrawTexture(indicator, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
            GUI.Box(indicator, GUIContent.none);
            tieBreakerStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                fontSize = 14
            };
            GUI.contentColor = Color.white;
            GUI.Label(indicator, header.Text, tieBreakerStyle);
            GUI.contentColor = previousContentColor;
        }

        private static Rect GetCardPanelRect()
        {
            return CalculateCardPanelRect(Screen.width, Screen.height);
        }

        public static Rect CalculateCardPanelRect(float viewportWidth, float viewportHeight)
        {
            var width = Mathf.Min(CardPanelWidth, Mathf.Max(0f, viewportWidth - (Margin * 2f)));
            var height = Mathf.Min(CardPanelHeight, Mathf.Max(0f, viewportHeight - (Margin * 2f)));
            return new Rect(
                Margin,
                Mathf.Max(Margin, viewportHeight - height - Margin),
                width,
                height);
        }

        private static Rect GetTieBreakerIndicatorRect()
        {
            var width = Mathf.Min(TieBreakerWidth, Mathf.Max(120f, Screen.width - (Margin * 2f)));
            return new Rect(
                Mathf.Max(Margin, Screen.width - width - Margin),
                Margin,
                width,
                TieBreakerHeight);
        }

        private static Rect GetRuleEventLogRect()
        {
            var cardPanel = GetCardPanelRect();
            var availableRightWidth = Screen.width - cardPanel.xMax - (Margin * 2f);
            if (availableRightWidth >= 420f)
            {
                var width = Mathf.Min(EventLogPreferredWidth, availableRightWidth);
                var height = Mathf.Min(
                    EventLogPreferredHeight,
                    Mathf.Max(160f, Screen.height - TieBreakerHeight - (Margin * 3f)));
                return new Rect(
                    Screen.width - width - Margin,
                    TieBreakerHeight + (Margin * 2f),
                    width,
                    height);
            }

            var availableAboveHeight = Mathf.Max(140f, cardPanel.y - (Margin * 2f));
            var aboveHeight = Mathf.Min(EventLogPreferredHeight, availableAboveHeight);
            return new Rect(
                cardPanel.x,
                Mathf.Max(Margin, cardPanel.y - aboveHeight - Margin),
                cardPanel.width,
                aboveHeight);
        }
    }
}
