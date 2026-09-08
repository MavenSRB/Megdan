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

    public sealed class UndoRestartViewModel
    {
        public UndoRestartViewModel(
            long version,
            bool canUndo,
            bool canRestart,
            int acceptedCommandCount,
            bool isHistoryAvailable)
        {
            Version = version;
            CanUndo = canUndo;
            CanRestart = canRestart;
            AcceptedCommandCount = acceptedCommandCount;
            IsHistoryAvailable = isHistoryAvailable;
        }

        public long Version { get; }
        public bool CanUndo { get; }
        public bool CanRestart { get; }
        public int AcceptedCommandCount { get; }

        /// <summary>
        /// False once a snapshot has been loaded, which permanently disables history. Both states
        /// leave <see cref="CanUndo"/> false, but only this one means no future command will ever
        /// make it true, so a view can say "history unavailable" rather than "nothing to undo".
        /// </summary>
        public bool IsHistoryAvailable { get; }
    }

    public interface IUndoRestartView
    {
        void Render(UndoRestartViewModel model);
    }

    public sealed class UndoRestartPresenter
    {
        private readonly IMatchSession session;
        private readonly IUndoRestartView view;
        private MatchSessionProjection lastProjection;
        private UndoRestartViewModel lastViewModel;

        public UndoRestartPresenter(IMatchSession session, IUndoRestartView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = new UndoRestartViewModel(
                    projection.Version,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count,
                    projection.IsHistoryAvailable);
            }

            view.Render(lastViewModel);
        }

        public bool Undo()
        {
            var result = session.Undo();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public bool Restart()
        {
            var result = session.Restart();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }
}
