using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114e. The words on every control Wuk's composed faces raise - the p.10 modal bullets
    /// and the "you may" / "up to" steps the decision window draws. Derived from
    /// <see cref="DecisionLabelRequirementQuery"/> over his dealt faces (the test asserts the two sets are
    /// equal), and joined to <see cref="DecisionLabelCatalog"/> at the dealing (8.114f), on the rule every
    /// hero's table follows: a table is for a window over a card a match deals. The trampling moves
    /// (8.114b1) and Dominance (8.114c1) add theirs when they are composed.
    /// </summary>
    public static partial class WukCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionArm(TreeSlam, "tree-slam-adjacent", "Target a minion adjacent to you");
            yield return DecisionArm(TreeSlam, "tree-slam-tree-arm",
                "Remove a Tree beside you, then target a unit in range");

            foreach (var face in new[]
                     {
                         WukRedFaces.NaturesProtector, WukRedFaces.NaturesGuardian, WukRedFaces.NaturesChampion
                     })
            {
                yield return DecisionArm(face, WukRedFaces.NatureAdjacentBulletNode, "Target a hero adjacent to you");
                yield return DecisionArm(face, WukRedFaces.NatureTreeBulletNode,
                    "Target a unit in range beside a Tree");
            }

            yield return DecisionStep(WukBlueFaces.GiftsOfNature, WukBlueFaces.SelfBulletNode,
                "Retrieve a discarded card", "Retrieve nothing");
            foreach (var face in new[] { WukBlueFaces.TreeOfPlenty, WukBlueFaces.Abundance })
            {
                // An arm of the modal that is itself a "you may": one declaration answers both roles.
                yield return DecisionStep(face, WukBlueFaces.SelfBulletNode,
                    "You retrieve a discarded card", "Retrieve nothing");
                yield return DecisionArm(face, WukBlueFaces.FriendBulletNode,
                    "A friendly hero in radius retrieves a card");
            }

            yield return DecisionStep(WukGreenFaces.MonstrousThrow, WukGreenFaces.ThrowRepeatNode,
                "Throw again", "Stop here");

            yield return DecisionArm(WukGreenFaces.IntoTheCanopy, WukGreenFaces.SwapWithTreeBulletNode(0),
                "Swap with a Tree in radius");
            yield return DecisionArm(WukGreenFaces.IntoTheCanopy, WukGreenFaces.SwapFriendBulletNode(0),
                "Swap a friendly unit with a Tree");

            for (var pass = 0; pass < 2; pass++)
            {
                yield return DecisionStep(WukGreenFaces.TreetopRide, $"swap-{pass}-up-to",
                    pass == 0 ? "Make a swap" : "Swap again", pass == 0 ? "No swap" : "Stop here");
                yield return DecisionArm(WukGreenFaces.TreetopRide, WukGreenFaces.SwapWithTreeBulletNode(pass),
                    "Swap with a Tree in radius");
                yield return DecisionArm(WukGreenFaces.TreetopRide, WukGreenFaces.SwapFriendBulletNode(pass),
                    "Swap a friendly unit with a Tree");
            }
        }
    }
}
