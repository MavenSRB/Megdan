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

    /// <summary>
    /// The `DIRECTOR` drawer's Undo and Reset. `RESET` is presentation terminology for the existing
    /// match Restart operation and forwards exactly that; nothing about what either does has changed.
    /// </summary>
    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        private readonly Button undo;
        private readonly Button restart;

        public UndoRestartUnityView()
        {
            Root = new VisualElement { name = "gd-director-tools" };
            Root.style.flexDirection = FlexDirection.Column;
            undo = HudElements.Control("UNDO", ForwardUndo, 28f);
            restart = HudElements.Control("RESET", ForwardRestart, 28f);
            restart.tooltip = "Restart the match from its initial state.";
            undo.style.marginBottom = 4f;
            Root.Add(undo);
            Root.Add(restart);
        }

        public VisualElement Root { get; }

        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;
        public event Action RestartRequested;

        /// <summary>
        /// What the `UNDO` control does, callable by a driver that has no pointer - the same idiom the
        /// minion prompt's <c>ForwardCandidate</c> and the round view's <c>ForwardInvocation</c> use, and
        /// for the same reason: a fixture and a build-smoke driver reach these surfaces without one.
        /// </summary>
        public void ForwardUndo() => UndoRequested?.Invoke();

        /// <summary>What the `RESET` control does. It is the existing Restart, under its HUD name.</summary>
        public void ForwardRestart() => RestartRequested?.Invoke();

        public void Render(UndoRestartViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            undo.SetEnabled(Model.CanUndo);
            undo.style.opacity = Model.CanUndo ? 1f : 0.45f;
            restart.SetEnabled(Model.CanRestart);
            restart.style.opacity = Model.CanRestart ? 1f : 0.45f;
        }
    }
}
