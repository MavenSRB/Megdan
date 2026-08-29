using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Arien on <see cref="StagedHeroBoard"/>. Milestone 8.29e, and the fifth hero to reach the shared
    /// fixture milestone 8.22 generalised out of Sabina's.
    ///
    /// <para>He is the last of the six to get one because milestone 8.20 dealt him before the fixture
    /// existed, so his cases were written against <see cref="ConfiguredMatchFixtures"/>' sessions.
    /// Milestone 8.29e needs what those sessions cannot state - one enemy unit beside a named target
    /// and nothing else within reach - which is precisely the affordance this fixture is.</para>
    /// </summary>
    internal static class ArienBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "arien",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusArienOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? ArienUpgradeBranch.SecondSibling
                    : ArienUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedArienTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedArienTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedArienForInternalTest(TeamSide.Red);
                }
            },
            ArienCardContent.LivingTsunami);

        /// <summary>
        /// The staged Wasp-versus-Arien match at the named p.15 stage, with the pieces the caller names
        /// relocated. Every parameter beyond the branch is <see cref="StagedHeroBoard.Create"/>'s and
        /// is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            ArienUpgradeBranch branch = ArienUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> arienDiscards = null,
            bool clearHisNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == ArienUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                arienDiscards,
                clearHisNeighbourhood);
    }
}
