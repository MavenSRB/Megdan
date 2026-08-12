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
            PlaceholderOneVsOneTestConfiguration initialConfiguration = null,
            ActiveEffectLifecycle initialTiming = null)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            InitialConfiguration = initialConfiguration;
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

        public PlaceholderOneVsOneTestConfiguration InitialConfiguration { get; }

        public ActiveEffectLifecycle InitialTiming { get; }

        public ActiveEffectLifecycle CreateTiming() => InitialTiming.Clone();

        public IReadOnlyList<BoardMapDefinition> AvailableMaps => availableMaps;
    }
}
