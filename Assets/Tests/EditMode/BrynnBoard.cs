using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Brynn the Seeker on <see cref="StagedHeroBoard"/>. Milestone 8.69g, the milestone that deals
    /// him - which is the first moment any fixture of his can drive a CONFIGURED MATCH rather than
    /// read his definitions or run a bounded effect frame.
    ///
    /// <para>His five composition blocks all had to prove their faces against hand-built boards and
    /// hand-built capability catalogs, because a configured match cannot deal a hero the build does
    /// not register. That is the debt each of them recorded and this one pays: Over the Top through a
    /// real snapshot and a real replay, and Familiar Ground's second bullet OFFERED or GREYED by the
    /// same board the player would be looking at. It is the same seven lines Misa's board, Tali's and
    /// Xargatha's are.</para>
    /// </summary>
    internal static class BrynnBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "brynn",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusBrynnOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? BrynnUpgradeBranch.SecondSibling
                    : BrynnUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedBrynnTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedBrynnTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedBrynnForInternalTest(TeamSide.Red);
                }
            },
            BrynnCardContent.OverTheTop);

        /// <summary>
        /// The staged Wasp-versus-Brynn match at the named p.15 stage, with the pieces the caller
        /// names relocated. Every parameter beyond the branch is <see cref="StagedHeroBoard.Create"/>'s
        /// and is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            BrynnUpgradeBranch branch = BrynnUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> brynnDiscards = null,
            bool clearHisNeighbourhood = true,
            IReadOnlyCollection<HexCoordinate> obstacles = null) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == BrynnUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                brynnDiscards,
                clearHisNeighbourhood,
                obstacles);
    }
}
