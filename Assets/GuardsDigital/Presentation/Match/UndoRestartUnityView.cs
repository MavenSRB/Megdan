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
    /// The `DIRECTOR` drawer's Undo control. Match restart remains an engine/session diagnostic,
    /// but is deliberately not exposed by either the PvP or PvE live Director tray.
    /// </summary>
    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        private readonly Button undo;
        public UndoRestartUnityView()
        {
            Root = new VisualElement { name = "gd-director-tools" };
            Root.style.flexDirection = FlexDirection.Column;
            undo = HudElements.Control("UNDO", ForwardUndo, 28f);
            Root.Add(undo);
        }

        public VisualElement Root { get; }

        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;

        /// <summary>
        /// What the `UNDO` control does, callable by a driver that has no pointer - the same idiom the
        /// minion prompt's <c>ForwardCandidate</c> and the round view's <c>ForwardInvocation</c> use, and
        /// for the same reason: a fixture and a build-smoke driver reach these surfaces without one.
        /// </summary>
        public void ForwardUndo() => UndoRequested?.Invoke();

        public void Render(UndoRestartViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            undo.SetEnabled(Model.CanUndo);
            undo.style.opacity = Model.CanUndo ? 1f : 0.45f;
        }
    }
}
