using System;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Match;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// W-021. A whole match composed outside this scene - the separate PvE executable's encounter is
    /// the first - handed to <see cref="MatchSceneCompositionRoot"/> as a one-shot so the scene's own
    /// views, HUD, board and runtime are built exactly as they are for every dealt match.
    /// </summary>
    /// <remarks>
    /// <para>It states the complete initial state, configuration included, so the root composes no
    /// format, line-up or setup of its own for it. Heroes the initial state leaves off the board are
    /// not drawn at load.</para>
    /// <para>No existing caller writes it; a scene load without it composes what it always has.</para>
    /// </remarks>
    public sealed class ExternalMatchComposition
    {
        public ExternalMatchComposition(
            MatchSessionInitialState initialState,
            Action<ConfiguredMatchSceneRuntime> composed = null)
        {
            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            if (initialState.InitialConfiguration == null)
            {
                throw new ArgumentException(
                    "An external composition states the configuration it was composed from.",
                    nameof(initialState));
            }

            Composed = composed;
        }

        public MatchSessionInitialState InitialState { get; }

        /// <summary>Invoked once with the runtime after the scene finished composing it.</summary>
        public Action<ConfiguredMatchSceneRuntime> Composed { get; }
    }
}
