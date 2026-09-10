using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Tali the Spiritcaller on <see cref="StagedHeroBoard"/>. Milestone 8.37a, which is the first Edit
    /// Mode fixture to need her staged match DRIVEN rather than only her definitions read.
    ///
    /// <para>Milestone 8.37 dealt her and added the configurations; every question it asked at this
    /// tier was about composed content, so it had no reason to add this. Milestone 8.37a's question is
    /// what a match does when Ice Blast is pointed at a minion - the one shape that reaches rulebook
    /// p.14's whole page inside a single command - so it needs the session the shared fixture builds.
    /// It is the same seven lines Misa's board and Xargatha's are.</para>
    /// </summary>
    internal static class TaliBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "tali",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusTaliOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? TaliUpgradeBranch.SecondSibling
                    : TaliUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedTaliTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedTaliTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedTaliForInternalTest(TeamSide.Red);
                }
            },
            TaliCardContent.ReignOfWinter);

        /// <summary>
        /// The staged Wasp-versus-Tali match at the named p.15 stage, with the pieces the caller names
        /// relocated. Every parameter beyond the branch is <see cref="StagedHeroBoard.Create"/>'s and
        /// is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            TaliUpgradeBranch branch = TaliUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> taliDiscards = null,
            bool clearHerNeighbourhood = true) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == TaliUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                taliDiscards,
                clearHerNeighbourhood);
    }
}
