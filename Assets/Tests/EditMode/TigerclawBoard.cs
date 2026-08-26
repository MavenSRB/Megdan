using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Tigerclaw on <see cref="StagedHeroBoard"/>. Milestone 8.23, and the first hero to use the shared
    /// fixture milestone 8.22 generalised out of Sabina's.
    ///
    /// <para>This is the whole of what a hero costs that fixture: which seat they take, which seat
    /// opposes them, how their three p.15 stages are configured, and which card p.16's unlock moves.
    /// Everything else - the board geometry, the commitment, the Attack and Defense steps, the answers,
    /// the readings - belongs to the staged position rather than to whoever is standing on it, and is
    /// shared. That the second hero needed no change to <c>StagedHeroBoard</c> at all is what the
    /// generalisation was for.</para>
    /// </summary>
    internal static class TigerclawBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "tigerclaw",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusTigerclawOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? TigerclawUpgradeBranch.SecondSibling
                    : TigerclawUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedTigerclawTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedTigerclawTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedTigerclawForInternalTest(TeamSide.Red);
                }
            },
            TigerclawCardContent.CloakAndDaggers);

        /// <summary>
        /// The staged Wasp-versus-Tigerclaw match at the named p.15 stage, with the pieces the caller
        /// names relocated. Every parameter beyond the branch is
        /// <see cref="StagedHeroBoard.Create"/>'s and is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            TigerclawUpgradeBranch branch = TigerclawUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> tigerclawDiscards = null,
            bool clearHisNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == TigerclawUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                tigerclawDiscards,
                clearHisNeighbourhood);
    }
}
