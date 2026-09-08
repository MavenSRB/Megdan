using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{

    /// <summary>The collapsible diagnostic log, toggled from the same `DIRECTOR` drawer.</summary>
    public sealed class DiagnosticLogUnityView : IDiagnosticLogView
    {
        private readonly Button toggle;
        private readonly Label panelLabel;
        private readonly ScrollView entries;

        public DiagnosticLogUnityView()
        {
            toggle = HudElements.Control("EVENT LOG", ForwardToggle, 28f);
            ToggleRoot = toggle;
            PanelRoot = HudElements.Panel(MatchHudTheme.PanelBackground);
            PanelRoot.name = "gd-event-log";
            PanelRoot.style.position = Position.Absolute;
            PanelRoot.style.display = DisplayStyle.None;
            PanelRoot.style.flexDirection = FlexDirection.Column;
            HudElements.Padding(PanelRoot, 10f);
            HudElements.Border(PanelRoot, MatchHudTheme.PanelBorder, 1f, 4f);
            panelLabel = HudElements.Text(string.Empty, 11f, MatchHudTheme.MutedText, FontStyle.Bold);
            entries = new ScrollView(ScrollViewMode.Vertical);
            entries.style.flexGrow = 1f;
            PanelRoot.Add(panelLabel);
            PanelRoot.Add(entries);
        }

        public VisualElement ToggleRoot { get; }
        public VisualElement PanelRoot { get; }

        public DiagnosticLogViewModel Model { get; private set; }

        public event Action ToggleRequested;

        /// <summary>What the `EVENT LOG` control does, for a driver that has no pointer.</summary>
        public void ForwardToggle() => ToggleRequested?.Invoke();

        public void Render(DiagnosticLogViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            toggle.text = Model.ToggleLabel;
            panelLabel.text = Model.PanelLabel;
            PanelRoot.style.display = Model.IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            entries.Clear();
            if (!Model.IsExpanded)
            {
                return;
            }

            foreach (var entry in Model.Entries)
            {
                entries.Add(HudElements.Text(entry.DisplayText, 11f, MatchHudTheme.PrimaryText));
            }
        }

        public void Layout(Rect panel)
        {
            PanelRoot.style.left = panel.x;
            PanelRoot.style.top = panel.y;
            PanelRoot.style.width = panel.width;
            PanelRoot.style.height = panel.height;
        }
    }
}
