using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Brogan's configured facade over the shared staged-hero board.</summary>
    internal static class BroganBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "brogan",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? BroganUpgradeBranch.SecondSibling
                    : BroganUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration.CreateStagedBroganTierThreeForInternalTest(
                            TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration.CreateStagedBroganTierTwoForInternalTest(
                            TeamSide.Red, branch);
                    default:
                        return MatchConfiguration.CreateStagedBroganForInternalTest(TeamSide.Red);
                }
            },
            BroganCardContent.OneManArmy);

        internal static StagedHeroBoard Create(
            int tier = 1,
            BroganUpgradeBranch branch = BroganUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool clearHisNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == BroganUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                clearTheNeighbourhood: clearHisNeighbourhood);
    }
}
