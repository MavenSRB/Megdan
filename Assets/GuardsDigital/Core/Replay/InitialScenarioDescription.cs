using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Replay
{
    public sealed class InitialScenarioDescription
    {
        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> initialCards;
        private readonly IReadOnlyList<HeroOccupancy> initialHeroes;
        private readonly IReadOnlyList<ObstacleOccupancy> initialObstacles;

        public InitialScenarioDescription(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> initialCards,
            TieBreakerState initialTieBreakerState,
            MapDefinitionReference selectedMap,
            IEnumerable<HeroOccupancy> initialHeroes,
            IEnumerable<ObstacleOccupancy> initialObstacles)
        {
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            this.definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            this.initialCards = Array.AsReadOnly(
                (initialCards ?? throw new ArgumentNullException(nameof(initialCards)))
                    .Select(CloneCard)
                    .ToArray());
            if (!initialTieBreakerState.IsDefined)
            {
                throw new ArgumentException("Initial scenario requires a chosen Tie Breaker result.", nameof(initialTieBreakerState));
            }
            if (string.IsNullOrWhiteSpace(selectedMap.MapId.Value) || selectedMap.ContentRevision <= 0)
            {
                throw new ArgumentException("Initial scenario requires selected map identity and revision.", nameof(selectedMap));
            }

            InitialTieBreakerState = initialTieBreakerState;
            SelectedMap = selectedMap;
            this.initialHeroes = Array.AsReadOnly(
                (initialHeroes ?? throw new ArgumentNullException(nameof(initialHeroes))).ToArray());
            this.initialObstacles = Array.AsReadOnly(
                (initialObstacles ?? throw new ArgumentNullException(nameof(initialObstacles))).ToArray());
        }

        public MatchParticipants Participants { get; }
        public TieBreakerState InitialTieBreakerState { get; }
        public MapDefinitionReference SelectedMap { get; }
        public IReadOnlyList<CardDefinition> CardDefinitions => definitions;
        public IReadOnlyList<CardInstance> InitialCards => initialCards.Select(CloneCard).ToArray();
        public IReadOnlyList<HeroOccupancy> InitialHeroes => initialHeroes;
        public IReadOnlyList<ObstacleOccupancy> InitialObstacles => initialObstacles;

        public PlaceholderOneVsOneTestConfiguration CreateConfiguration()
        {
            return PlaceholderOneVsOneTestConfiguration.Restore(
                new PlaceholderConfigurationRestoreState(
                    Participants,
                    definitions,
                    initialCards.Select(CloneCard),
                    InitialTieBreakerState));
        }

        public AdjacentMoveAuthority CreateBoard(IEnumerable<BoardMapDefinition> availableMaps)
        {
            return CreateBoard(availableMaps, Enumerable.Empty<QuickGameMinionSetupState>());
        }

        public AdjacentMoveAuthority CreateBoard(
            IEnumerable<BoardMapDefinition> availableMaps,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            var matches = (availableMaps ?? throw new ArgumentNullException(nameof(availableMaps)))
                .Where(map => map.Reference.Equals(SelectedMap))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Initial scenario map {SelectedMap.MapId.Value} revision {SelectedMap.ContentRevision} is unavailable or ambiguous.");
            }

            return new AdjacentMoveAuthority(matches[0], initialHeroes, initialObstacles, minions);
        }

        private static CardInstance CloneCard(CardInstance card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            return new CardInstance(card.Id, card.DefinitionReference, card.OwnerId, card.State);
        }
    }
}
