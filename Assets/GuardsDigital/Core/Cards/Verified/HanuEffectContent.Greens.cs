using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's five Greens - Monkey Trick, Monkey Twist, Hear Nothing, Monkey Business and
    /// See Nothing - which print two sentences between them and differ only in what follows the first.
    /// Milestone 8.71f.
    ///
    /// <para><b>The two sentences.</b> "Swap two friendly units in radius" (Monkey Trick, Monkey Twist,
    /// Monkey Business) and "Swap with an enemy hero in radius" (Hear Nothing, See Nothing). They are
    /// two factories rather than one with a relation parameter because they are not one sentence with a
    /// side written into it: the first names TWO pieces neither of which is Hanu and exchanges them
    /// with each other, the second names ONE and exchanges it with him. Different questions, different
    /// number of objects, different consuming Swap - <c>synthetic-swap@2</c> over two chosen spaces
    /// against <c>synthetic-swap-actor@1</c> over one.</para>
    ///
    /// <para><b>What follows them.</b> Monkey Business and See Nothing print "Move up to 1 space" and
    /// the other three print nothing. p.11 makes that a mandatory step whose DISTANCE is the decision
    /// with zero admitted - <see cref="EffectFailurePolicy.UpTo"/>, Garrus's GA-Q1 and Brynn's Hide
    /// Traces - and not a Movement action, so no movement-action restriction bars it and no
    /// movement-action buff widens it (HA-Q10).</para>
    ///
    /// <para><b>Where each trailing move starts.</b> See Nothing's starts where the exchange left HIM,
    /// bound from the Swap's own <c>actorAt</c> rather than by asking the board again - Brynn's BR-Q20d
    /// binding, and Whisper's Cruel Twist and Arien's Ebb and Flow before it. Monkey Business's starts
    /// where he has been standing all along, because the sentence he just performed moved two OTHER
    /// units: <c>synthetic-actor-space@1</c> is the bridge that publishes a hero's own square, and
    /// placing it after the exchange rather than before is what makes "after the swap" true of the
    /// definition as well as of the text.</para>
    ///
    /// <para><b>An impossible swap stops the action and the trailing move is never reached.</b> The
    /// selection and the exchange are both mandatory steps of one sequence, so a board on which the
    /// sentence names nobody is p.10's impossible mandatory step: the action performs nothing and the
    /// later sibling is never asked. That is Rowenna's Defend the Innocent shape and Brynn's three
    /// swaps', stated by ordering rather than by a condition this definition invents.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // "Swap two friendly units in radius."
        // -----------------------------------------------------------------------------------------

        /// <summary>The sequence every friendly-pair face roots on.</summary>
        public const string FriendlySwapRootNode = "green-friendly-swap-root";

        /// <summary>The one question the sentence asks, over the pair it names.</summary>
        public const string FriendlySwapPairNode = "green-friendly-swap-pair";

        /// <summary>p.19's exchange of the two spaces the pair published.</summary>
        public const string FriendlySwapExchangeNode = "green-friendly-swap-exchange";

        /// <summary>
        /// The bridge that publishes Hanu's own square for the trailing move - present on Monkey
        /// Business alone, because it is the only friendly-pair face that prints a second sentence.
        /// </summary>
        public const string FriendlySwapActorSpaceNode = "green-friendly-swap-actor-space";

        /// <summary>"Move up to 1 space", on the faces that print it.</summary>
        public const string FriendlySwapMoveNode = "green-friendly-swap-move";

        // -----------------------------------------------------------------------------------------
        // "Swap with an enemy hero in radius."
        // -----------------------------------------------------------------------------------------

        public const string EnemySwapRootNode = "green-enemy-swap-root";

        public const string EnemySwapSubjectNode = "green-enemy-swap-subject";

        public const string EnemySwapExchangeNode = "green-enemy-swap-exchange";

        public const string EnemySwapMoveNode = "green-enemy-swap-move";

        /// <summary>The space the exchange leaves Hanu standing in.</summary>
        public const string SwapActorAtOutput = "actorAt";

        /// <summary>The space the exchange leaves the enemy hero standing in.</summary>
        public const string SwapSubjectAtOutput = "subjectAt";

        /// <summary>The two spaces the friendly exchange traded.</summary>
        public const string SwapFirstAtOutput = "firstAt";

        public const string SwapSecondAtOutput = "secondAt";

        /// <summary>The square <c>synthetic-actor-space@1</c> publishes.</summary>
        public const string ActorSpaceOutput = "space";

        /// <summary>Where a trailing move left him.</summary>
        public const string SwapMovedOutput = "moved";

        /// <summary>
        /// "Swap two friendly units in radius[. Move up to 1 space]" - Monkey Trick at Radius 1, Monkey
        /// Twist at 2 and Monkey Business at 2 with the second sentence.
        ///
        /// <para><paramref name="moveDistance"/> of zero is the two faces that print no second
        /// sentence. One method and no overload of it, on milestone 8.57's lesson: an overload
        /// re-points every IL rule that names the method.</para>
        ///
        /// <para>The reach is nowhere in this definition, which is the point of
        /// <see cref="HanuComponentContent.FriendlyUnitPairV1"/>: p.9 puts the Radius on the card and
        /// p.16 raises it with the item, so a number written down here would be a second reading of an
        /// icon the selection already reads.</para>
        /// </summary>
        public static EffectDefinition FriendlySwap(
            CardDefinitionId cardId, string contentRevision, int moveDistance)
        {
            var root = new EffectNodeId(FriendlySwapRootNode);
            var pair = new EffectNodeId(FriendlySwapPairNode);
            var exchange = new EffectNodeId(FriendlySwapExchangeNode);
            var actorSpace = new EffectNodeId(FriendlySwapActorSpaceNode);
            var move = new EffectNodeId(FriendlySwapMoveNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: moveDistance > 0
                        ? new[] { pair, exchange, actorSpace, move }
                        : new[] { pair, exchange }),

                new EffectNodeDefinition(
                    pair, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    HanuComponentContent.FriendlyUnitPairV1,
                    outputs: new[]
                    {
                        // All six, because the selection publishes whichever kind of unit was named in
                        // each slot and an undeclared recorded outcome is a snapshot this build
                        // refuses to read back - the shipped pair publishes the same six for the same
                        // reason.
                        new EffectOutputBinding(
                            HanuComponentContent.FirstSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            HanuComponentContent.SecondSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            HanuComponentContent.FirstHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            HanuComponentContent.FirstMinionOutputName, EffectValueKind.Minion),
                        new EffectOutputBinding(
                            HanuComponentContent.SecondHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            HanuComponentContent.SecondMinionOutputName, EffectValueKind.Minion)
                    }),

                new EffectNodeDefinition(
                    exchange, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "first", pair, HanuComponentContent.FirstSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromNodeOutput(
                            "second", pair, HanuComponentContent.SecondSpaceOutputName,
                            EffectValueKind.Coordinate),

                        // "units", which p.19 makes heroes and minions and never a token.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SwapFirstAtOutput, EffectValueKind.Coordinate),
                        new EffectOutputBinding(SwapSecondAtOutput, EffectValueKind.Coordinate)
                    })
            };

            if (moveDistance > 0)
            {
                nodes.Add(new EffectNodeDefinition(
                    actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                    }));
                nodes.Add(Move(move, actorSpace, ActorSpaceOutput, moveDistance));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Swap with an enemy hero in radius[. Move up to 1 space]" - Hear Nothing at Radius 3 and See
        /// Nothing at 3 with the second sentence.
        ///
        /// <para>Two nodes and then the move: <c>synthetic-radius-hero-target@1</c> names the hero
        /// inside the performing card's own printed Radius with p.16's items composed on, and
        /// <c>synthetic-swap-actor@1</c> exchanges him with Hanu. It is the actor-fixed Swap rather
        /// than the general one for the reason that capability records - the actor is in the frame, so
        /// a walk judging whether this card can do anything asks the same question the choice will ask,
        /// and a hero is never offered whose Swap would then be refused (8.35l's consumer
        /// declaration).</para>
        ///
        /// <para>p.19 makes the exchange a TARGETING of that hero, so p.18's immunity narrows the offer
        /// here; the relation does the rest, since "an enemy hero" is a side and Hanu is not on it.</para>
        /// </summary>
        public static EffectDefinition EnemySwap(
            CardDefinitionId cardId, string contentRevision, int moveDistance)
        {
            var root = new EffectNodeId(EnemySwapRootNode);
            var subject = new EffectNodeId(EnemySwapSubjectNode);
            var exchange = new EffectNodeId(EnemySwapExchangeNode);
            var move = new EffectNodeId(EnemySwapMoveNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: moveDistance > 0
                        ? new[] { subject, exchange, move }
                        : new[] { subject, exchange }),

                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        // A Skill's effect is the action (p.12), so the immunity question this
                        // selection asks is the Skill's own - and the exchange behind it asks p.19's
                        // Swap as well.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),

                        // Neither face prints "not adjacent to you", so nothing is taken out of the
                        // disk's innermost ring.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),

                        // "an ENEMY hero" is a side he is not on, so the self-inclusion Silverarrow's
                        // faces turn on is stated off here rather than left unsaid.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(exchange))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                new EffectNodeDefinition(
                    exchange, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", subject,
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate),

                        // "an enemy hero", which p.19 makes a unit.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SwapActorAtOutput, EffectValueKind.Coordinate),
                        new EffectOutputBinding(SwapSubjectAtOutput, EffectValueKind.Coordinate)
                    })
            };

            if (moveDistance > 0)
            {
                nodes.Add(Move(move, exchange, SwapActorAtOutput, moveDistance));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Move up to 1 space" - his own card-text move, from the square the node before it published.
        ///
        /// <para>Neither face names where he may go, so neither declares a policy narrower than p.11's
        /// own; <see cref="EffectFailurePolicy.UpTo"/> is what makes standing still one of the answers
        /// without making the step declinable.</para>
        /// </summary>
        private static EffectNodeDefinition Move(
            EffectNodeId id, EffectNodeId origin, string originOutput, int distance) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", origin, originOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SwapMovedOutput, EffectValueKind.Coordinate)
                });

        /// <summary>
        /// Every decision label the five Greens declare, which is NONE - derived rather than asserted:
        /// <see cref="DecisionLabelRequirementQuery"/> names exactly two kinds of control, the arms of
        /// a <c>Choose</c> and the nodes <c>OptionalStepQuery.OpensConfirmOrDecline</c> answers for,
        /// and these five bodies contain neither. Both trailing moves are <c>UpTo</c> capability nodes,
        /// which pause to name a DESTINATION rather than to confirm or decline - Brynn's swaps and
        /// Silverarrow's whole set declare nothing for the same reason. <c>HanuGreensTests</c> holds
        /// this against the query rather than against a list, so a face that starts asking something
        /// cannot arrive wordless.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> GreenDecisionLabels() =>
            System.Array.Empty<DecisionLabelDeclaration>();

        // MILESTONE 8.71g FOLDED `GreenSkillEffectId` AWAY, exactly as this block's own remark asked:
        // it minted `<card>-skill` and so did three sibling blocks' spellings of the same string.
        // `HanuEffectContent.SkillEffectId` is the single one now.
    }
}
