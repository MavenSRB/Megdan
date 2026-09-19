using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The placements Min the Dragonmonk's three Blue mine faces perform. Milestone 8.61d.
    ///
    /// <para><b>What is composed here, and what is deliberately not.</b> Each face's sentence has two
    /// halves. The first - "Place [N] mine tokens, [k] blast and [m] dud, facedown in radius; units may
    /// move through them" - is this file, and every word of it is either a node below or a declaration
    /// on the token in <see cref="MinComponentContent"/>. The second - "After an enemy hero moves
    /// through a mine, remove it; for every blast removed, that hero discards a card, if able" - is
    /// milestone 8.61e's, and it is ABSENT rather than stubbed: a placeholder node that removed nothing
    /// and asked nobody for a card would be a composition claiming to be the printed rule. The debt is
    /// written down in <see cref="MineTrapOwed"/>, and <c>MinMineTests</c> asserts both halves of
    /// it.</para>
    ///
    /// <para><b>The seam 8.61e attaches to is the displacement, not this effect.</b> The trap fires
    /// "after an enemy hero MOVES THROUGH a mine", which is a fact about a route somebody else walks
    /// on some later turn - not a step of the Skill that placed it. So nothing here reserves a node for
    /// it; what it needs is the board authority's own commits, and the placed instances it will be
    /// asked about are the ones <c>SyntheticTokenCapabilities.PlacedOutput</c> already publishes.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// The block-scoped placeholder content revision. Milestone 8.61i composes Min's set and mints
        /// the real one; this string is deliberately not shaped like a landed revision id, so it can
        /// never be mistaken for one.
        /// </summary>
        public const string MineRevision = "min-8-61d-mines";

        /// <summary>
        /// The trap each of the three faces still owes, and the milestone that owes it.
        ///
        /// <para><b>It is deliberately NOT a <c>PrintedTextComposition</c> debt.</b> That vocabulary
        /// says "this action's printed text has no composition", and <c>ActionDefinition</c> refuses it
        /// beside an effect root because the two together would be two answers to one question. The
        /// trap is not one of this Skill's steps at all: it fires when some other hero walks a route on
        /// a later turn, so it is a standing rule the PLACED PIECE carries, which milestone 8.61e hangs
        /// off the board authority's own displacement commits. The debt is therefore recorded here, as
        /// what it is, and <c>MinMineTests</c> asserts both halves - that all three faces are named,
        /// and that nothing in the placements below removes a mine or asks anyone for a card.</para>
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, string> MineTrapOwed =>
            LazyMineTrapOwed.Value;

        private static readonly Lazy<IReadOnlyDictionary<CardDefinitionId, string>>
            LazyMineTrapOwed = new Lazy<IReadOnlyDictionary<CardDefinitionId, string>>(() =>
                new Dictionary<CardDefinitionId, string>
                {
                    [MinCardContent.TripMine] = MineTrapOwedTo,
                    [MinCardContent.ClusterMine] = MineTrapOwedTo,
                    [MinCardContent.MineField] = MineTrapOwedTo
                });

        /// <summary>The one milestone all three debts name.</summary>
        public const string MineTrapOwedTo = "milestone 8.61e, the mine trap";

        /// <summary>
        /// The placement body for one mine face, or <c>null</c> for a card this file does not own.
        /// </summary>
        public static EffectDefinition MineSkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(MinCardContent.TripMine))
            {
                return CreateMinePlacements(cardId, contentRevision, blasts: 1, duds: 1);
            }

            if (cardId.Equals(MinCardContent.ClusterMine))
            {
                return CreateMinePlacements(cardId, contentRevision, blasts: 1, duds: 2);
            }

            if (cardId.Equals(MinCardContent.MineField))
            {
                return CreateMinePlacements(cardId, contentRevision, blasts: 2, duds: 1);
            }

            return null;
        }

        /// <summary>
        /// "Place [N] mine tokens, [k] blast and [m] dud, facedown in radius", as one reclamation and
        /// one placement per token.
        ///
        /// <para><b>Two nodes per placement, in p.18's own order.</b> The page puts the short-supply
        /// step first - "When a card tells you to place a token, take it from the supply first. If you
        /// need to place more tokens than are available in the supply, first remove up to that number
        /// of tokens of the same type from the board, if able" - so each placement is a reclamation
        /// followed by a placement, the reclamation declining when the pile already covers it and
        /// carrying the page's own <c>if able</c> policy. Because the pile is shared, that reclamation
        /// may take back a standing dud to make room for a blast, which is what one type means.</para>
        ///
        /// <para><b>Every placement is mandatory, because the text prints none of p.10's three
        /// softeners.</b> "Place 2 mine tokens" carries no "you may", no "up to" and no "if able", and
        /// p.10 is explicit that "all other steps are mandatory". A mandatory step that cannot be
        /// performed stops the enclosing sequence with the prior work kept, which is exactly "place as
        /// many as possible": empty spaces in radius only get scarcer as the copies go down, so the
        /// first impossible placement is the point past which none of the rest could have happened
        /// either.</para>
        ///
        /// <para><b>Blasts are placed before duds, which is MI-Q10d.</b> Min chooses the SPACE for each
        /// copy - p.10's "you make all the choices" - and the composition decides the ORDER, because
        /// the card prints one: "1 blast and 1 dud", "1 blast and 2 duds", "2 blasts and 1 dud". The
        /// order is only observable on a board with fewer empty spaces in radius than the card places,
        /// and there the printed order is the only reading that does not invent a decision the card
        /// never offers.</para>
        ///
        /// <para>One node per copy rather than one node asked twice, for the reason milestone 6.2 split
        /// p.18's short-supply step out and <c>SyntheticTokenCapabilities</c> restates: a handler is
        /// stateless and cannot tell a second resumption from a first, so a second decision belongs to
        /// a second node. It is also what lets each copy name its own kind.</para>
        /// </summary>
        private static EffectDefinition CreateMinePlacements(
            CardDefinitionId cardId, string contentRevision, int blasts, int duds)
        {
            var root = new EffectNodeId("place-root");
            var nodes = new List<EffectNodeDefinition>();
            var rootChildren = new List<EffectNodeId>();

            for (var index = 0; index < blasts + duds; index++)
            {
                var place = index < blasts
                    ? MinComponentContent.PlaceMineBlastV1
                    : MinComponentContent.PlaceMineDudV1;
                var pair = new EffectNodeId($"place-pair-{index}");
                var reclaimNode = new EffectNodeId($"place-reclaim-{index}");
                var placeNode = new EffectNodeId($"place-mine-{index}");

                nodes.Add(new EffectNodeDefinition(
                    pair, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { reclaimNode, placeNode }));
                nodes.Add(new EffectNodeDefinition(
                    reclaimNode, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    MinComponentContent.ReclaimMineV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            Effects.Synthetic.SyntheticTokenCapabilities.ReclaimedOutput,
                            EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    placeNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, place,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            Effects.Synthetic.SyntheticTokenCapabilities.PlacedOutput,
                            EffectValueKind.Coordinate)
                    }));
                rootChildren.Add(pair);
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: rootChildren));

            return new EffectDefinition(
                new EffectDefinitionId(cardId.Value + "-skill"), contentRevision, root, nodes);
        }
    }
}
