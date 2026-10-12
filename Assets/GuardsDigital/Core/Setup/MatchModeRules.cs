using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>W-022. How a scheduled off-board hero reaches the board at a round's start.</summary>
    public enum ModeHeroPlacementKind
    {
        None = 0,

        /// <summary>The hero's player chooses among the empty placement spaces.</summary>
        PlayerChoice = 1,

        /// <summary>The hero enters on its designated space, or the nearest empty one (ties offered).</summary>
        Designated = 2
    }

    /// <summary>
    /// W-022. Where card effects read the mode rules the match is currently bound to; the round
    /// authority is the one source, so a rebind after restore is seen by every reader at once.
    /// </summary>
    public interface IMatchModeRulesSource
    {
        MatchModeRules ModeRules { get; }
    }

    /// <summary>W-022. One hero a mode places at the start of a round.</summary>
    public sealed class ModeHeroPlacement
    {
        public ModeHeroPlacement(HeroId heroId, int round, ModeHeroPlacementKind kind, HexCoordinate? designatedSpace = null)
        {
            if (!heroId.IsDefined) throw new ArgumentException("A placement names a hero.", nameof(heroId));
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            if (kind != ModeHeroPlacementKind.PlayerChoice && kind != ModeHeroPlacementKind.Designated)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == ModeHeroPlacementKind.Designated && !designatedSpace.HasValue)
                throw new ArgumentException("A designated placement names its space.", nameof(designatedSpace));
            HeroId = heroId;
            Round = round;
            Kind = kind;
            DesignatedSpace = designatedSpace;
        }

        public HeroId HeroId { get; }
        public int Round { get; }
        public ModeHeroPlacementKind Kind { get; }
        public HexCoordinate? DesignatedSpace { get; }
    }

    /// <summary>
    /// W-022. The rules a separate game mode composes a match with - the PvE executable's
    /// (PVE-D03, D11, D12, D14, D18, D19, D22) is the first. Every member is a statement the existing
    /// mode never makes: a match composed without mode rules (the <c>null</c> every existing caller
    /// passes) runs exactly the rules it always has.
    /// </summary>
    /// <remarks>
    /// <para>The rules travel on the match's initial state and are bound again after every rebuild
    /// and every restore - restart, Undo, rollback and snapshot restore alike - and are never
    /// serialized, so no snapshot schema or wire meaning moves.</para>
    /// <para>The two system transitions it declares are begun with the ordinary Begin System
    /// Transition command and answered with the ordinary answer command, so no command vocabulary
    /// moves either.</para>
    /// </remarks>
    public sealed class MatchModeRules
    {
        public const string PlacementTransitionPrefix = "mode-round-start-placement/round-";

        public static readonly SystemTransitionStepId PlacementStepId =
            new SystemTransitionStepId("mode-round-start-placement-place");

        public MatchModeRules(
            IEnumerable<EndOfRoundStage> unavailableEndOfRoundStages = null,
            bool coinGainsAddNothing = false,
            TeamSide respawnOverrideSide = TeamSide.None,
            IEnumerable<HexCoordinate> respawnOverrideSpaces = null,
            IEnumerable<HexCoordinate> playerPlacementSpaces = null,
            IEnumerable<ModeHeroPlacement> roundStartPlacements = null,
            TeamSide defeatInsteadOfDiscardSide = TeamSide.None,
            IEnumerable<KeyValuePair<HeroId, StandardMinionType>> minionCategoryHeroes = null,
            IEnumerable<HeroId> boardRequiredTurnHeroes = null)
        {
            UnavailableEndOfRoundStages = Array.AsReadOnly(
                (unavailableEndOfRoundStages ?? Enumerable.Empty<EndOfRoundStage>()).Distinct().ToArray());
            if (UnavailableEndOfRoundStages.Any(stage => stage == EndOfRoundStage.Complete ||
                    !Enum.IsDefined(typeof(EndOfRoundStage), stage)))
                throw new ArgumentException("Only executable end-of-round steps can be made unavailable.",
                    nameof(unavailableEndOfRoundStages));
            CoinGainsAddNothing = coinGainsAddNothing;
            RespawnOverrideSide = respawnOverrideSide;
            RespawnOverrideSpaces = Array.AsReadOnly(
                (respawnOverrideSpaces ?? Enumerable.Empty<HexCoordinate>()).Distinct().ToArray());
            if ((RespawnOverrideSide != TeamSide.None) != (RespawnOverrideSpaces.Count > 0))
                throw new ArgumentException("A respawn override names both its side and its spaces.",
                    nameof(respawnOverrideSpaces));
            PlayerPlacementSpaces = Array.AsReadOnly(
                (playerPlacementSpaces ?? Enumerable.Empty<HexCoordinate>()).Distinct().ToArray());
            RoundStartPlacements = Array.AsReadOnly(
                (roundStartPlacements ?? Enumerable.Empty<ModeHeroPlacement>()).ToArray());
            if (RoundStartPlacements.Select(placement => placement.HeroId).Distinct().Count() != RoundStartPlacements.Count)
                throw new ArgumentException("A hero is placed by the mode at most once.", nameof(roundStartPlacements));
            if (RoundStartPlacements.Any(placement => placement.Kind == ModeHeroPlacementKind.PlayerChoice) &&
                PlayerPlacementSpaces.Count == 0)
                throw new ArgumentException("A player-chosen placement needs placement spaces.", nameof(playerPlacementSpaces));
            DefeatInsteadOfDiscardSide = defeatInsteadOfDiscardSide;
            var categories = (minionCategoryHeroes ?? Enumerable.Empty<KeyValuePair<HeroId, StandardMinionType>>()).ToArray();
            if (categories.Any(pair => !pair.Key.IsDefined || pair.Value == StandardMinionType.None ||
                    !Enum.IsDefined(typeof(StandardMinionType), pair.Value)))
                throw new ArgumentException("Mode minion categories require defined identities and classes.", nameof(minionCategoryHeroes));
            MinionCategoryHeroes = new ReadOnlyDictionary<HeroId, StandardMinionType>(categories.ToDictionary(pair => pair.Key, pair => pair.Value));
            BoardRequiredTurnHeroes = Array.AsReadOnly((boardRequiredTurnHeroes ?? Enumerable.Empty<HeroId>()).Distinct().ToArray());
            if (BoardRequiredTurnHeroes.Any(hero => !hero.IsDefined))
                throw new ArgumentException("Turn participation requires defined heroes.", nameof(boardRequiredTurnHeroes));
        }

        /// <summary>These mode units participate only while on the board at commitment time.</summary>
        public IReadOnlyList<HeroId> BoardRequiredTurnHeroes { get; }

        public IEnumerable<HeroId> TurnHeroes(MatchParticipants participants,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int roundIndex,
            TurnCardCommitmentRestoreState state = null) => participants.Heroes.Select(hero => hero.Id)
            .Where(hero => !BoardRequiredTurnHeroes.Contains(hero) || board.IsHeroOnBoard(hero) ||
                (state != null && !RoundStartPlacements.Any(placement => placement.HeroId.Equals(hero) && placement.Round > roundIndex) &&
                    (state.Commitments.Any(pair => pair.Key.Equals(hero)) || state.Passes.Contains(hero))));

        /// <summary>PVE-D30: additional targeting/effect category, never a second board identity or combat model.</summary>
        public IReadOnlyDictionary<HeroId, StandardMinionType> MinionCategoryHeroes { get; }

        public bool HasMinionCategory(HeroId hero) => MinionCategoryHeroes.ContainsKey(hero);

        public StandardMinionType MinionCategory(HeroId hero) =>
            MinionCategoryHeroes.TryGetValue(hero, out var kind) ? kind : StandardMinionType.None;

        /// <summary>PVE-D03. End-of-round steps this mode does not run.</summary>
        public IReadOnlyList<EndOfRoundStage> UnavailableEndOfRoundStages { get; }

        /// <summary>PVE-D11/D14/D22. Every coin gain is accepted and adds nothing.</summary>
        public bool CoinGainsAddNothing { get; }

        /// <summary>PVE-D18. The side whose heroes respawn onto <see cref="RespawnOverrideSpaces"/>.</summary>
        public TeamSide RespawnOverrideSide { get; }

        public IReadOnlyList<HexCoordinate> RespawnOverrideSpaces { get; }

        /// <summary>PVE-D05. The spaces a player-chosen placement offers.</summary>
        public IReadOnlyList<HexCoordinate> PlayerPlacementSpaces { get; }

        /// <summary>PVE-D05/D12. The off-board heroes placed at a round's start, and how.</summary>
        public IReadOnlyList<ModeHeroPlacement> RoundStartPlacements { get; }

        /// <summary>PVE-D19. The side whose forced discards take a Defense card or defeat it instead.</summary>
        public TeamSide DefeatInsteadOfDiscardSide { get; }

        public IEnumerable<int> PlacementRounds =>
            RoundStartPlacements.Select(placement => placement.Round).Distinct().OrderBy(round => round);

        public static SystemTransitionDefinitionId PlacementTransitionId(int round) =>
            new SystemTransitionDefinitionId(PlacementTransitionPrefix + round.ToString(CultureInfo.InvariantCulture));

        public static bool TryReadPlacementRound(SystemTransitionDefinitionId id, out int round)
        {
            round = 0;
            return id.IsDefined && id.Value.StartsWith(PlacementTransitionPrefix, StringComparison.Ordinal) &&
                int.TryParse(id.Value.Substring(PlacementTransitionPrefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out round) && round >= 1;
        }

        /// <summary>The transitions this mode adds to a match's catalog.</summary>
        public IEnumerable<SystemTransitionDefinition> TransitionDefinitions =>
            PlacementRounds.Select(round => new SystemTransitionDefinition(
                    PlacementTransitionId(round),
                    "Round " + round.ToString(CultureInfo.InvariantCulture) + " Placement",
                    new List<SystemTransitionStepDefinition> { SystemTransitionStepDefinition.Derived(PlacementStepId) }));

        public bool IsUnavailable(EndOfRoundStage stage) => UnavailableEndOfRoundStages.Contains(stage);
    }
}
