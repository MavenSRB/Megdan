using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Application
{
    public sealed class MatchSessionInitialState
    {
        private readonly IReadOnlyList<BoardMapDefinition> availableMaps;

        public MatchSessionInitialState(
            InitialScenarioDescription scenario,
            QuickGameSetupState setup,
            IEnumerable<BoardMapDefinition> availableMaps,
            MatchConfiguration initialConfiguration = null,
            ActiveEffectLifecycle initialTiming = null,
            MatchStagedProgression stagedProgression = null)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            InitialConfiguration = initialConfiguration;
            StagedProgression = stagedProgression ?? MatchStagedProgression.None;
            InitialTiming = (initialTiming ?? new ActiveEffectLifecycle()).Clone();
            this.availableMaps = Array.AsReadOnly(
                (availableMaps ?? throw new ArgumentNullException(nameof(availableMaps))).ToArray());

            if (this.availableMaps.Count == 0)
            {
                throw new ArgumentException("A match session requires at least one available map.", nameof(availableMaps));
            }

            if (!Scenario.SelectedMap.Equals(Setup.SelectedMap))
            {
                throw new ArgumentException("The configured setup and initial scenario must select the same map.", nameof(setup));
            }

            if (!Scenario.InitialTieBreakerState.Equals(Setup.TieBreakerState))
            {
                throw new ArgumentException("The configured setup and initial scenario must share Tie Breaker state.", nameof(setup));
            }

            var matchingMaps = this.availableMaps.Count(map => map.Reference.Equals(Scenario.SelectedMap));
            if (matchingMaps != 1)
            {
                throw new ArgumentException("The initial scenario map must resolve exactly once.", nameof(availableMaps));
            }

        }

        public InitialScenarioDescription Scenario { get; }

        public QuickGameSetupState Setup { get; }

        public MatchConfiguration InitialConfiguration { get; }

        /// <summary>
        /// Milestone 8.35z17. The Levels and coin balances this match begins from, and
        /// <see cref="MatchStagedProgression.None"/> for every match that begins from the ones
        /// rulebook p.15 deals.
        ///
        /// <para>It belongs on the initial state rather than beside it because this is what the
        /// aggregate rebuilds from: a restart and a retained-prefix Undo both re-run
        /// <c>RebuildInitialState</c>, so a staged Level that lived anywhere else would be lost by
        /// the first Undo and a staged board would silently become an unstaged one. Being here is
        /// exactly what makes a staged situation replayable.</para>
        /// </summary>
        public MatchStagedProgression StagedProgression { get; }

        public ActiveEffectLifecycle InitialTiming { get; }

        public ActiveEffectLifecycle CreateTiming() => InitialTiming.Clone();

        public IReadOnlyList<BoardMapDefinition> AvailableMaps => availableMaps;
    }
}
