using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Misa the Samurai on <see cref="StagedHeroBoard"/>. Milestone 8.35x-confirmfix, which is the
    /// first Edit Mode fixture to need her staged match driven end to end rather than only her
    /// definitions read.
    ///
    /// <para>This is the whole of what a hero costs that fixture: which seat they take, which seat
    /// opposes them, how their three p.15 stages are configured, and which card p.16's unlock moves.
    /// Milestone 8.35z38 dealt her and added the configurations; it had no reason to add this,
    /// because every question it asked was about composed definitions. The confirm corrective's
    /// questions are about what a match DOES when one of her placements resolves, so it needs the
    /// session the shared fixture builds.</para>
    ///
    /// <para><b>Her Tier III configuration already unlocks the Ultimate</b>, which is why nothing here
    /// passes <c>unlockUltimate</c>: Power Overwhelming's raised modal ceiling is live on the staged
    /// Tier III arm as composed, measured before this fixture was written.</para>
    /// </summary>
    internal static class MisaBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "misa",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusMisaOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? MisaUpgradeBranch.SecondSibling
                    : MisaUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedMisaTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedMisaTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedMisaForInternalTest(TeamSide.Red);
                }
            },
            MisaCardContent.PowerOverwhelming);

        /// <summary>
        /// The staged Wasp-versus-Misa match at the named p.15 stage, with the pieces the caller names
        /// relocated. Every parameter beyond the branch is <see cref="StagedHeroBoard.Create"/>'s and
        /// is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            MisaUpgradeBranch branch = MisaUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> misaDiscards = null,
            bool clearHerNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == MisaUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                misaDiscards,
                clearHerNeighbourhood);
    }
}
