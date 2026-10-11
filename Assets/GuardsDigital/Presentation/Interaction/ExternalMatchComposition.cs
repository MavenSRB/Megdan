using System;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
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
            Action<ConfiguredMatchSceneRuntime> composed = null,
            Func<IMatchHost, IMatchSession, IMatchSession> sessionDecorator = null,
            TeamSide? directorPlayerSide = null)
        {
            SessionDecorator = sessionDecorator;
            DirectorPlayerSide = directorPlayerSide;
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

        /// <summary>
        /// W-023. Wraps the Director session the runtime composes, so every surface - the HUD, Undo
        /// and Restart included - talks to the mode's session. Null keeps the Director session.
        /// </summary>
        public Func<IMatchHost, IMatchSession, IMatchSession> SessionDecorator { get; }

        /// <summary>
        /// W-026. The one side the Director plays while the mode's own controller plays the other:
        /// the tray, turn controls, decision windows and board outlines read from that side only.
        /// Null keeps the Director answering for every hero.
        /// </summary>
        public TeamSide? DirectorPlayerSide { get; }
    }
}
