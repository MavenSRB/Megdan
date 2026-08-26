using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Xargatha on <see cref="StagedHeroBoard"/>. Milestone 8.24, and the third hero to use the shared
    /// fixture milestone 8.22 generalised out of Sabina's.
    ///
    /// <para>This is the whole of what a hero costs that fixture: which seat they take, which seat
    /// opposes them, how their three p.15 stages are configured, and which card p.16's unlock moves.
    /// Everything else - the board geometry, the commitment, the Attack and Defense steps, the answers,
    /// the readings - belongs to the staged position rather than to whoever is standing on it, and is
    /// shared.</para>
    ///
    /// <para><b>She is the first hero whose fixtures usually want their minions left where they
    /// are.</b> <see cref="StagedHeroBoard.Create"/>'s neighbourhood-clearing default exists so an
    /// Attack's target is unambiguous; five of her faces are answered only by an adjacent or in-radius
    /// minion, and three of her scaled values are computed from how many are standing beside her. Her
    /// fixtures therefore pass <c>clearHerNeighbourhood: false</c> far more often than any earlier
    /// hero's, and each one that does says which minions it is counting on.</para>
    /// </summary>
    internal static class XargathaBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "xargatha",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusXargathaOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? XargathaUpgradeBranch.SecondSibling
                    : XargathaUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedXargathaTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedXargathaTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return PlaceholderOneVsOneTestConfiguration
                            .CreateStagedXargathaForInternalTest(TeamSide.Red);
                }
            },
            XargathaCardContent.Metamorphosis);

        /// <summary>
        /// The staged Wasp-versus-Xargatha match at the named p.15 stage, with the pieces the caller
        /// names relocated. Every parameter beyond the branch is
        /// <see cref="StagedHeroBoard.Create"/>'s and is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            XargathaUpgradeBranch branch = XargathaUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> xargathaDiscards = null,
            bool clearHerNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == XargathaUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                xargathaDiscards,
                clearHerNeighbourhood);
    }
}
