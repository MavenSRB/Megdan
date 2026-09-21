using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's Hurry up! - his Silver basic, and the one face milestone 8.71a composes.
    ///
    /// <para><b>The sentence.</b> "Set the printed [Initiative] Initiative value of an unresolved card
    /// of a hero in range to 11, until it is resolved or otherwise changes state. ( This may change
    /// the initiative order and tie breakers )." The parenthesis is a reminder rather than a rule -
    /// p.7 already re-checks the Initiative after every resolution - so nothing is composed for
    /// it.</para>
    ///
    /// <para><b>Two nodes, in one mandatory sequence.</b> The selection
    /// (<c>synthetic-range-hero-target@1</c>) is the sentence's object and the rewrite
    /// (<see cref="HanuComponentContent.HurryUpInitiativeV1"/>) is its verb, which is the division
    /// every targeting face in this build already draws. Both are mandatory: a board on which no hero
    /// in range holds an unresolved card is p.10's impossible mandatory step, so the action performs
    /// nothing and the primary greys at the offer the way Brynn's Familiar Ground greys on an empty
    /// board (milestone 8.70).</para>
    ///
    /// <para><b>What the selection declares, clause by clause.</b> "a hero" with no side named is
    /// <see cref="SourceTeamRelation.AnyTeam"/> - the card qualifies nobody, so neither does this; "in
    /// range" is the printed Range 4 icon with p.16's Range items composed on, which is what that
    /// capability reads; Hanu himself is out because <c>includeSelf</c> is unbound and p.10 says an
    /// action affects you only when the text says so; an immune hero is out because p.19 makes this a
    /// target and the selection asks p.18 at the offer; and "unresolved card" is 8.71a's one new
    /// narrowing. <c>excludeTheAttacker</c> is bound FALSE and not merely omitted, because the Range
    /// arm requires it: this card is not a Block rider and names no attacker (HA-Q1).</para>
    ///
    /// <para><b>Nothing is asked twice.</b> The rewrite takes the hero as an input and finds their
    /// unresolved card live, through the same query the selection greyed by and the same query p.7's
    /// turn order reads - so a hero this face offers is a hero this face can rewrite.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        /// <summary>The mandatory sequence the whole face is.</summary>
        public const string HurryUpRootNode = "hanu-hurry-up-root";

        /// <summary>"a hero in range" holding an unresolved card - the selection.</summary>
        public const string HurryUpTargetNode = "hanu-hurry-up-target";

        /// <summary>
        /// "Set the printed Initiative value ... to 11" - the node that performs the rewrite, and
        /// <b>the hook milestone 8.71b rides</b>.
        ///
        /// <para>The Ultimate Trick prints "You choose the next action, and how it is performed for a
        /// hero you target with the 'Hurry Up!'". The hero it means is the one this node published
        /// <see cref="HurryUpTargetOutputName"/> for, in this resolution - not the one the selection
        /// offered, because a selection can be answered and the rewrite still fail, and not the hero
        /// the card names anywhere else, because the card names them nowhere else.</para>
        /// </summary>
        public const string HurryUpRewriteNode = "hanu-hurry-up-rewrite";

        /// <summary>
        /// The output name 8.71b binds on <see cref="HurryUpRewriteNode"/> to learn which hero Hurry
        /// up! targeted. Spelled here as well as on the capability so a consumer written against this
        /// face reads the face's own constant.
        /// </summary>
        public const string HurryUpTargetOutputName = HanuComponentContent.TargetedHeroOutputName;

        /// <summary>The card instance whose printed icon was rewritten, published beside the hero.</summary>
        public const string HurryUpRewrittenCardOutputName =
            HanuComponentContent.RewrittenCardOutputName;

        /// <summary>The hero the selection chose, before the rewrite is attempted.</summary>
        public const string HurryUpChosenHeroOutputName =
            SyntheticTargetingCapabilities.ChosenHeroOutputName;

        /// <summary>
        /// The number the card prints. It is declared here, once, so the definition and every fixture
        /// that measures it read the same eleven.
        /// </summary>
        public const int HurryUpPrintedInitiative = 11;

        /// <summary>
        /// Hurry up!'s card text, as one mandatory sequence of two steps.
        /// </summary>
        public static EffectDefinition HurryUp(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(HurryUpRootNode);
            var target = new EffectNodeId(HurryUpTargetNode);
            var rewrite = new EffectNodeId(HurryUpRewriteNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { target, rewrite }),

                new EffectNodeDefinition(
                    target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1,
                    inputs: new[]
                    {
                        // "a hero" - the card names no side, so neither does this (HA-Q1).
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AnyTeam),

                        // p.18's activity this card text runs as: the enclosing Skill.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),

                        // Required on this arm and FALSE here: "other than the attacker" is a Block
                        // rider's clause and this face is not one.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                            EffectValue.Boolean(false)),

                        // "an unresolved card of a hero in range" - milestone 8.71a's one narrowing.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(HurryUpChosenHeroOutputName, EffectValueKind.Hero)
                    }),

                new EffectNodeDefinition(
                    rewrite, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    HanuComponentContent.HurryUpInitiativeV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            HanuComponentContent.HurryUpHeroParameterName,
                            target,
                            HurryUpChosenHeroOutputName,
                            EffectValueKind.Hero),

                        // The card's own printed eleven, declared at the call site.
                        EffectInputBinding.FromLiteral(
                            HanuComponentContent.HurryUpPrintedInitiativeParameterName,
                            EffectValue.Integer(HurryUpPrintedInitiative))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(HurryUpTargetOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            HurryUpRewrittenCardOutputName, EffectValueKind.CardInstance)
                    })
            };

            // MILESTONE 8.71g: the id was this literal until his set had one place to spell it.
            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// NONE, and that is this block's measured result rather than an omission.
        ///
        /// <para>Hurry up! raises no confirm-or-decline and no modal arm: both of its steps are
        /// mandatory, its one prompt is a selection whose candidates name themselves, and
        /// <c>OptionalStepQuery</c> is what decides - the same reason Silverarrow's whole set declares
        /// nothing. Derived rather than asserted: <c>HanuHurryUpTests</c> asks
        /// <see cref="DecisionLabelRequirementQuery"/> which controls this face raises and holds this
        /// list against it, so a later composition that starts asking something cannot arrive
        /// wordless.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> HurryUpDecisionLabels() =>
            System.Array.Empty<DecisionLabelDeclaration>();
    }
}
