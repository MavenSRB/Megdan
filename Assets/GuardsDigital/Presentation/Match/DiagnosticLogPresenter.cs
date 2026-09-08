using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{

    public sealed class DiagnosticLogViewModel
    {
        public DiagnosticLogViewModel(long version, bool isExpanded, IEnumerable<RuleEventLogEntry> entries)
        {
            Version = version;
            IsExpanded = isExpanded;
            Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
            ToggleLabel = IsExpanded ? "HIDE EVENT LOG" : $"EVENT LOG ({Entries.Count})";
            PanelLabel = $"INTERNAL RULE-EVENT LOG - {Entries.Count} ACCEPTED EVENTS";
        }

        public long Version { get; }
        public bool IsExpanded { get; }
        public IReadOnlyList<RuleEventLogEntry> Entries { get; }
        public string ToggleLabel { get; }
        public string PanelLabel { get; }
    }

    public interface IDiagnosticLogView
    {
        void Render(DiagnosticLogViewModel model);
    }

    public sealed class DiagnosticLogPresenter
    {
        private readonly IMatchSession session;
        private readonly IDiagnosticLogView view;
        private bool isExpanded;
        private MatchSessionProjection lastProjection;
        private bool lastExpanded;
        private DiagnosticLogViewModel lastViewModel;

        public DiagnosticLogPresenter(IMatchSession session, IDiagnosticLogView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection) || isExpanded != lastExpanded)
            {
                lastProjection = projection;
                lastExpanded = isExpanded;
                lastViewModel = new DiagnosticLogViewModel(
                    projection.Version,
                    isExpanded,
                    projection.DiagnosticEvents);
            }

            view.Render(lastViewModel);
        }

        public void Toggle()
        {
            isExpanded = !isExpanded;
            Refresh();
        }
    }
}
