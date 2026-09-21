using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Hanu the Trickster on <see cref="StagedHeroBoard"/>. Milestone 8.71h, the milestone that deals
    /// him - which is the first moment any fixture of his can drive a CONFIGURED MATCH rather than
    /// read his definitions or run a bounded effect frame.
    ///
    /// <para>His six composition blocks all had to prove their faces against hand-built boards and
    /// hand-built capability catalogs, because a configured match cannot deal a hero the build does
    /// not register. That is the debt each of them recorded and this one pays: Hurry up! rewriting an
    /// enemy's printed Initiative in a live turn order, The Ultimate Trick re-homing that hero's next
    /// action to his own seat, and a journey standing its Hero-scoped effect through a real snapshot.
    /// It is the same seven lines Brynn's board, Misa's, Tali's and Xargatha's are.</para>
    ///
    /// <para>The one board this facade cannot reach is the ALLY board - Arien standing beside him at
    /// <c>blue-hero-02</c>, which is what This Way! and That Way! need and which
    /// <see cref="MatchConfiguration.CreateStagedHanuWithAllyForInternalTest"/> composes. That is a
    /// three-seat setup rather than a relocation of this one, so a fixture whose subject is a co-move
    /// names it directly; everything else of his lives here.</para>
    /// </summary>
    internal static class HanuBoard
    {
        internal static readonly StagedHeroSpec Spec = new StagedHeroSpec(
            "hanu",
            "wasp",
            QuickGameSetupConfiguration.CreateStagedWaspVersusHanuOneVsOne,
            (tier, secondSibling) =>
            {
                var branch = secondSibling
                    ? HanuUpgradeBranch.SecondSibling
                    : HanuUpgradeBranch.FirstSibling;
                switch (tier)
                {
                    case 3:
                        return MatchConfiguration
                            .CreateStagedHanuTierThreeForInternalTest(TeamSide.Red, branch);
                    case 2:
                        return MatchConfiguration
                            .CreateStagedHanuTierTwoForInternalTest(TeamSide.Red, branch);
                    default:
                        return MatchConfiguration
                            .CreateStagedHanuForInternalTest(TeamSide.Red);
                }
            },
            HanuCardContent.TheUltimateTrick);

        /// <summary>
        /// The staged Wasp-versus-Hanu match at the named p.15 stage, with the pieces the caller
        /// names relocated. Every parameter beyond the branch is <see cref="StagedHeroBoard.Create"/>'s
        /// and is documented there.
        /// </summary>
        internal static StagedHeroBoard Create(
            int tier = 1,
            HanuUpgradeBranch branch = HanuUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> hanuDiscards = null,
            bool clearHisNeighbourhood = true,
            IReadOnlyCollection<HexCoordinate> obstacles = null) =>
            StagedHeroBoard.Create(
                Spec,
                tier,
                branch == HanuUpgradeBranch.SecondSibling,
                minionPlacements,
                heroPlacements,
                banishedMinions,
                unlockUltimate,
                enemyHandSize,
                hanuDiscards,
                clearHisNeighbourhood,
                obstacles);
    }
}
