using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Sabina on <see cref="StagedHeroBoard"/>. Milestone 8.22.
    ///
    /// <para>This is the whole of what a hero costs the shared fixture: which seat they take, which
    /// seat opposes them, how their three p.15 stages are configured, and which card p.16's unlock
    /// moves. Everything else - the board geometry, the commitment, the Attack and Defense steps, the
    /// answers, the readings - belongs to the staged position rather than to whoever is standing on
    /// it, and is shared.</para>
    ///
    /// <para>The upgrade branch stays her own enum here and is handed over as "the second sibling or
    /// not", because a shared fixture cannot name <c>SabinaUpgradeBranch</c>,
    /// <c>BroganUpgradeBranch</c> and the four still to come - and the caller should not have to
    /// remember which of them is which either.</para>
    /// </summary>
    internal static class SabinaBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "sabina",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusSabinaOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? SabinaUpgradeBranch.SecondSibling
                    : SabinaUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedSabinaTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedSabinaTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedSabinaForInternalTest(TeamSide.Red);
                }
            },
            SabinaCardContent.BigSoddingGun);

        /// <summary>
        /// The staged Wasp-versus-Sabina match at the named p.15 stage, with the pieces the caller
        /// names relocated. Every parameter beyond the branch is
        /// <see cref="StagedHeroBoard.Create"/>'s and is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            SabinaUpgradeBranch branch = SabinaUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> sabinaDiscards = null,
            bool clearHerNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == SabinaUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                sabinaDiscards,
                clearHerNeighbourhood);
    }
}
