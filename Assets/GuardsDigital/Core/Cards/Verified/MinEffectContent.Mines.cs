using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The placements Min the Dragonmonk's three Blue mine faces perform. Milestone 8.61d.
    ///
    /// <para><b>What is composed here, and what is deliberately elsewhere.</b> Each face's sentence has
    /// two halves. The first - "Place [N] mine tokens, [k] blast and [m] dud, facedown in radius; units
    /// may move through them" - is <see cref="MineSkillEffectFor"/> below, and every word of it is
    /// either a node there or a declaration on the token in <see cref="MinComponentContent"/>. The
    /// second - "After an enemy hero moves through a mine, remove it; for every blast removed, that
    /// hero discards a card, if able" - is milestone 8.61e's, and it is still not a node of the Skill,
    /// because it never could be: it fires on a route somebody else walks on a later turn. It is
    /// declared on the placed piece instead and applied by <c>MoveThroughTrapOperation</c> off the
    /// board's own displacements; <see cref="MineTrapEffects"/> is the only part of it that is an
    /// effect at all, and it is what the removal COSTS rather than the removal itself.</para>
    ///
    /// <para><b>The seam is the displacement, not this effect.</b> Nothing in the placements reserves a
    /// node for the trap, and <c>MinMineTests</c> still asserts that: the graph below removes no mine
    /// and asks nobody for a card. The placed instances the trap is asked about are the ones
    /// <c>SyntheticTokenCapabilities.PlacedOutput</c> already publishes.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// Where the trap each of the three faces prints actually lives, now that milestone 8.61e has
        /// built it - and it is deliberately still not a node of the placements below.
        ///
        /// <para><b>It is not one of this Skill's steps and could never have been.</b> "After an enemy
        /// hero moves through a mine, remove it; for every blast removed, that hero discards a card, if
        /// able" fires when SOMEBODY ELSE walks a route, possibly several turns later and possibly
        /// never. So it is a standing rule the PLACED PIECE carries -
        /// <c>RuleModificationKind.RemovedWhenAnEnemyHeroMovesThrough</c> on all three mine faces, and
        /// <c>TokenDefinition.MoveThroughTrapConsequences</c> on the blast alone - and the rule is
        /// applied by <c>MoveThroughTrapOperation</c> off the board's own displacements. This
        /// dictionary is what each face's sentence resolved to, kept so a reader of the composition
        /// below is told where the other half went rather than left to conclude it is missing.</para>
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, string> MineTrapLivesOn =>
            LazyMineTrapLivesOn.Value;

        private static readonly Lazy<IReadOnlyDictionary<CardDefinitionId, string>>
            LazyMineTrapLivesOn = new Lazy<IReadOnlyDictionary<CardDefinitionId, string>>(() =>
                new Dictionary<CardDefinitionId, string>
                {
                    [MinCardContent.TripMine] = MineTrapCarriedBy,
                    [MinCardContent.ClusterMine] = MineTrapCarriedBy,
                    [MinCardContent.MineField] = MineTrapCarriedBy
                });

        /// <summary>The one piece all three sentences hang their trap on.</summary>
        public const string MineTrapCarriedBy = "the placed mine token, milestone 8.61e";

        /// <summary>
        /// "for every blast removed, that hero discards a card, if able", as one effect per number of
        /// blasts one crossing can remove at once.
        ///
        /// <para><b>Why a count picks a whole effect rather than parameterising one.</b> Each discard
        /// is p.10's own decision taken by the crossing hero on their own seat, so two blasts are two
        /// questions - and the engine runs ONE trigger frame, whose nodes are content. A frame that
        /// assembled its own nodes when it fired would also be a frame no snapshot could restore: a
        /// restore looks its effect up in the trusted catalog by reference, and an effect that existed
        /// only for the moment it ran is in no catalog. So the list is declared, indexed by count, and
        /// bounded by the pile - <see cref="MinComponentContent.MineSupplyCount"/> copies exist, so no
        /// route can spring more than that many at once.</para>
        ///
        /// <para><b>The hero is a role, not a selection</b> - Bain's A Complicated Profession exactly:
        /// the crossing already named them, so a one-candidate prompt here would invent a decision the
        /// card does not print. <b>"if able" is the skipping kind</b>, revision 1 of the discard, so an
        /// empty-handed hero gives up nothing and is NOT defeated - the contrast the two Grenades print
        /// in as many words ("discards a card, <b>or is defeated</b>") and these three do not.</para>
        /// </summary>
        public static IEnumerable<EffectDefinition> MineTrapEffects() =>
            Enumerable
                .Range(1, MinComponentContent.MineSupplyCount)
                .Select(MineTrapEffect)
                .ToArray();

        /// <summary>
        /// The effect that prices a crossing which removed exactly <paramref name="blasts"/>. It is
        /// authored at <see cref="MinCardContent.ContentRevision"/> rather than at a caller's revision,
        /// because the blast's own declaration names these by reference and a reference whose revision
        /// a caller could choose would be a link that stopped resolving.
        /// </summary>
        public static EffectDefinition MineTrapEffect(int blasts)
        {
            if (blasts < 1 || blasts > MinComponentContent.MineSupplyCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blasts), blasts,
                    "One crossing removes at least one blast and never more than the pile holds.");
            }

            var root = new EffectNodeId("mine-trap-root");
            var nodes = new List<EffectNodeDefinition>();
            var children = new List<EffectNodeId>();
            for (var index = 0; index < blasts; index++)
            {
                var discard = new EffectNodeId($"mine-trap-discard-{index}");
                children.Add(discard);
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticCardStateCapabilities.HeroParameter,
                            EffectRoleKind.OpposingHero,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children));
            return new EffectDefinition(
                MinComponentContent.MineTrapEffectId(blasts),
                MinCardContent.ContentRevision,
                root,
                nodes);
        }

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
