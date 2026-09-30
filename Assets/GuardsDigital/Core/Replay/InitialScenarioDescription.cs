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
        private readonly IReadOnlyList<HeroBodySupply> heroBodySupplies;

        /// <summary>
        /// A scenario whose Tie Breaker side was explicitly selected rather than flipped, so there is
        /// no draw to record. Every focused fixture names the side it wants, which is exactly the
        /// `DEV-005` shape; the configured match uses the overload that carries its recorded seed.
        /// </summary>
        public InitialScenarioDescription(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> initialCards,
            TieBreakerState initialTieBreakerState,
            MapDefinitionReference selectedMap,
            IEnumerable<HeroOccupancy> initialHeroes,
            IEnumerable<ObstacleOccupancy> initialObstacles,
            IEnumerable<HeroBodySupply> heroBodySupplies = null)
            : this(
                participants,
                definitions,
                initialCards,
                initialTieBreakerState,
                SetupRandomnessSeed.Unrecorded,
                selectedMap,
                initialHeroes,
                initialObstacles,
                heroBodySupplies)
        {
        }

        public InitialScenarioDescription(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> initialCards,
            TieBreakerState initialTieBreakerState,
            SetupRandomnessSeed setupSeed,
            MapDefinitionReference selectedMap,
            IEnumerable<HeroOccupancy> initialHeroes,
            IEnumerable<ObstacleOccupancy> initialObstacles,
            IEnumerable<HeroBodySupply> heroBodySupplies = null)
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
            SetupSeed = setupSeed;
            SelectedMap = selectedMap;
            this.initialHeroes = Array.AsReadOnly(
                (initialHeroes ?? throw new ArgumentNullException(nameof(initialHeroes))).ToArray());
            this.initialObstacles = Array.AsReadOnly(
                (initialObstacles ?? throw new ArgumentNullException(nameof(initialObstacles))).ToArray());
            // Milestone 9.21a. The heroes that begin the scenario on several bodies, with their supply.
            // No configured match declares any; a staged scenario that does is what lets a many-bodied
            // board be restarted, rewound and replayed exactly as it began.
            this.heroBodySupplies = Array.AsReadOnly(
                (heroBodySupplies ?? Enumerable.Empty<HeroBodySupply>()).ToArray());
        }

        public MatchParticipants Participants { get; }
        public TieBreakerState InitialTieBreakerState { get; }

        /// <summary>
        /// The recorded setup draw <see cref="InitialTieBreakerState"/> was derived from, or
        /// <see cref="SetupRandomnessSeed.Unrecorded"/> when the side was explicitly selected under
        /// `DEV-005`. Milestone 8.28b.
        /// </summary>
        public SetupRandomnessSeed SetupSeed { get; }
        public MapDefinitionReference SelectedMap { get; }
        public IReadOnlyList<CardDefinition> CardDefinitions => definitions;
        public IReadOnlyList<CardInstance> InitialCards => initialCards.Select(CloneCard).ToArray();
        public IReadOnlyList<HeroOccupancy> InitialHeroes => initialHeroes;
        public IReadOnlyList<ObstacleOccupancy> InitialObstacles => initialObstacles;
        public IReadOnlyList<HeroBodySupply> HeroBodySupplies => heroBodySupplies;

        /// <summary>
        /// Milestone 9.21j. The same scenario with <paramref name="declared"/> added for every hero it
        /// does not already declare a supply for - how a dealt many-bodied hero's roster declaration
        /// reaches his board. A supply the scenario states itself wins. Returns this scenario when
        /// nothing is added, so a setup that seats no many-bodied hero is the scenario it was.
        /// </summary>
        public InitialScenarioDescription WithHeroBodySupplies(IEnumerable<HeroBodySupply> declared)
        {
            var added = (declared ?? throw new ArgumentNullException(nameof(declared)))
                .Where(supply => heroBodySupplies.All(existing => !existing.HeroId.Equals(supply.HeroId)))
                .ToArray();
            if (added.Length == 0)
            {
                return this;
            }

            return new InitialScenarioDescription(
                Participants,
                definitions,
                initialCards,
                InitialTieBreakerState,
                SetupSeed,
                SelectedMap,
                initialHeroes,
                initialObstacles,
                heroBodySupplies.Concat(added));
        }

        public MatchConfiguration CreateConfiguration()
        {
            return MatchConfiguration.Restore(
                new MatchConfigurationRestoreState(
                    Participants,
                    definitions,
                    initialCards.Select(CloneCard),
                    InitialTieBreakerState,
                    SetupSeed));
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

            return new AdjacentMoveAuthority(
                matches[0], initialHeroes, initialObstacles, minions, heroBodySupplies);
        }

        private static CardInstance CloneCard(CardInstance card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            return new CardInstance(card.Id, card.DefinitionReference, card.OwnerId, card.State);
        }
    }
}
