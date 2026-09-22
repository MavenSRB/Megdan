using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's effect bodies - milestone 8.75. Every one of her action cards is composed as
    /// a coin-branch: a single <see cref="EffectNodeKind.Conditional"/> whose predicate is
    /// <c>ignatia-coin-sun@1</c> (<see cref="IgnatiaComponentContent.CoinSunV1"/>), its then-child the
    /// sun (☀) text and its else-child the moon (☾) text. <see cref="CoinBranched"/> builds that root;
    /// each shard partial builds the two arm subtrees from existing capabilities.
    ///
    /// <para>The shard partials (<c>IgnatiaEffectContent.Reds.cs</c> etc.) each expose one
    /// <c>&lt;Card&gt;Body(string)</c> factory per card; <see cref="PrimaryBody"/> dispatches a card id
    /// to its factory, and <c>IgnatiaCardContent</c> attaches the result to the printed primary. Nothing
    /// here is registered - milestone 7.9's gate - until the dealing block flips it.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        /// <summary>The coin Conditional node id, one per definition (unique within it).</summary>
        public const string CoinGateNode = "ignatia-coin-gate";

        /// <summary>The effect-definition id a card's printed primary text composes under.</summary>
        public static EffectDefinitionId PrimaryEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-text");

        /// <summary>
        /// Wraps a card's two arm subtrees in the coin Conditional. The sun arm runs when the revealed
        /// Tie Breaker face is her sun (the predicate publishes true), the moon arm otherwise. The two
        /// arm roots must appear in <paramref name="armNodes"/>; every node id must be unique within the
        /// returned definition.
        /// </summary>
        public static EffectDefinition CoinBranched(
            CardDefinitionId cardId,
            string contentRevision,
            EffectNodeId sunArm,
            EffectNodeId moonArm,
            IEnumerable<EffectNodeDefinition> armNodes)
        {
            var gate = new EffectNodeId(CoinGateNode);
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    gate,
                    EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    IgnatiaComponentContent.CoinSunV1,
                    childNodeIds: new[] { sunArm, moonArm },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            IgnatiaComponentContent.SunOutput, EffectValueKind.Boolean)
                    })
            };
            nodes.AddRange(armNodes);
            return new EffectDefinition(PrimaryEffectId(cardId), contentRevision, gate, nodes);
        }

        /// <summary>
        /// The printed-primary effect body for a card, or <c>null</c> for a face that composes none yet
        /// (the Ultimate, whose rule is a standing ability, and any card still awaiting its slice).
        /// Each shard partial contributes its cards through <see cref="ShardBody"/>; this is the one
        /// integration point, filled shard by shard as the composition lands.
        /// </summary>
        public static EffectDefinition PrimaryBody(CardDefinitionId cardId, string contentRevision) =>
            ShardBody(cardId, contentRevision);

        /// <summary>
        /// Dispatches a card id to its shard's body factory. Implemented once per shard as a chain of
        /// <c>?? </c> fall-throughs the shards each extend; returns <c>null</c> for a card no shard has
        /// composed yet.
        /// </summary>
        private static EffectDefinition ShardBody(CardDefinitionId cardId, string contentRevision) =>
            RedBody(cardId, contentRevision) ??
            BlueBody(cardId, contentRevision) ??
            GreenBody(cardId, contentRevision) ??
            BasicBody(cardId, contentRevision);

        // Each shard partial implements exactly one of these; the placeholders here keep the root
        // compiling before a shard lands and are replaced by the shard's own partial-method body.
        static partial void RedBodyImpl(CardDefinitionId cardId, string revision, ref EffectDefinition body);
        static partial void BlueBodyImpl(CardDefinitionId cardId, string revision, ref EffectDefinition body);
        static partial void GreenBodyImpl(CardDefinitionId cardId, string revision, ref EffectDefinition body);
        static partial void BasicBodyImpl(CardDefinitionId cardId, string revision, ref EffectDefinition body);

        private static EffectDefinition RedBody(CardDefinitionId cardId, string revision)
        {
            EffectDefinition body = null;
            RedBodyImpl(cardId, revision, ref body);
            return body;
        }

        private static EffectDefinition BlueBody(CardDefinitionId cardId, string revision)
        {
            EffectDefinition body = null;
            BlueBodyImpl(cardId, revision, ref body);
            return body;
        }

        private static EffectDefinition GreenBody(CardDefinitionId cardId, string revision)
        {
            EffectDefinition body = null;
            GreenBodyImpl(cardId, revision, ref body);
            return body;
        }

        private static EffectDefinition BasicBody(CardDefinitionId cardId, string revision)
        {
            EffectDefinition body = null;
            BasicBodyImpl(cardId, revision, ref body);
            return body;
        }
    }
}
