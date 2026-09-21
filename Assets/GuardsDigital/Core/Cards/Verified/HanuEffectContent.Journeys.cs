using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's three Blue journeys - Unexpected Journey, There and Back Again and Safe
    /// Travels - which print one clause between them and differ only in the Radius, the item and
    /// whether a move follows. Milestone 8.71d.
    ///
    /// <para><b>The clause.</b> "Swap with an enemy hero in radius. <b>This turn:</b> That hero is
    /// immune. <b>End of turn:</b> Swap with that hero regardless of radius and immunity." Safe
    /// Travels adds "Move up to 1 space." after the End-of-turn sentence.</para>
    ///
    /// <para><b>Two bodies, because the card names two moments.</b> The Skill's own body is the first
    /// sentence and the arming of the other two: choose an enemy hero in the printed Radius, exchange
    /// places with them, and create the Active effect that both carries the immunity and holds the
    /// work owed at the turn's close. The second body is what that effect runs when the turn ends.
    /// Nothing is carried between them except the hero, and the hero is carried by the effect rather
    /// than by either body - see <see cref="HanuComponentContent.JourneyPartner"/>.</para>
    ///
    /// <para><b>The first swap is measured and targeted; the second is neither.</b>
    /// <c>synthetic-radius-hero-target@1</c> reads the performing card's printed Radius with p.16's
    /// items composed on, narrowed to the opposing side, and p.19 makes the exchange a TARGETING of
    /// the hero - so an immune enemy is not a candidate, and the selection declares the exchange as
    /// its consumer so a hero the Swap would refuse is never offered (milestone 8.35l). The
    /// end-of-turn exchange asks neither question: "regardless of radius" is free, because
    /// <c>synthetic-swap-actor@1</c> performs no reach check at all and the effect draws no disk; and
    /// "regardless of immunity" is one <see cref="ImmunityOverrideDeclaration"/> on that node. p.18
    /// writes its own keyword to be overridden in those very words - "unless the card text says
    /// otherwise" - and <see cref="ImmunityOverrideReason.PrintedUnqualifiedImmunity"/> is the reason
    /// this build already mints for printed text that says so without qualifying it (HA-Q13d).</para>
    ///
    /// <para><b>Nothing here needs a "was the first swap taken" condition.</b> Both nodes of the Skill
    /// body are mandatory steps of one sequence, so a board with no enemy hero in the disk is p.10's
    /// impossible mandatory step: the action performs nothing, the creation is never reached, and no
    /// effect stands to fire at the turn's end. That is Brynn's tracking swap and Rowenna's Defend the
    /// Innocent - ordering rather than a condition this definition invents.</para>
    ///
    /// <para><b>The triggered body is mandatory throughout, and that is HA-Q11 built rather than
    /// restated.</b> The swap back "is mandatory and skips no reach or immunity check"; it is nothing
    /// when either hero is off the board, which is the partner lookup finding nobody and p.10 stopping
    /// there; and it is cancelled when Hanu is defeated first, which is p.17's
    /// <c>ControllerHeroDefeated</c> on the effect and never a clause here.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        // -------------------------------------------------------------------------------------------
        // The Skill's own body - "Swap with an enemy hero in radius. This turn: That hero is immune."
        // -------------------------------------------------------------------------------------------

        /// <summary>The sequence all three faces root their Skill on.</summary>
        public const string JourneyRootNode = "journey-root";

        /// <summary>"an enemy hero in radius" - the one selection the clause makes.</summary>
        public const string JourneySubjectNode = "journey-subject";

        /// <summary>"Swap with" - p.19's exchange, first object fixed to Hanu.</summary>
        public const string JourneyExchangeNode = "journey-exchange";

        /// <summary>The Active effect the two keywords share. See <see cref="HanuActiveEffectContent"/>.</summary>
        public const string JourneyEffectNode = "journey-effect";

        /// <summary>The space the first exchange leaves Hanu standing in.</summary>
        public const string JourneyActorAtOutput = "actorAt";

        /// <summary>The space the first exchange leaves the enemy hero standing in.</summary>
        public const string JourneySubjectAtOutput = "subjectAt";

        // -------------------------------------------------------------------------------------------
        // The end-of-turn body - "Swap with that hero regardless of radius and immunity[. Move up
        // to 1 space]."
        // -------------------------------------------------------------------------------------------

        public const string JourneyBackRootNode = "journey-back-root";

        /// <summary>"that hero" - the referent, read off the occurrence that is firing.</summary>
        public const string JourneyBackPartnerNode = "journey-back-partner";

        /// <summary>"Swap with that hero regardless of radius and immunity".</summary>
        public const string JourneyBackExchangeNode = "journey-back-exchange";

        /// <summary>
        /// Safe Travels' "Move up to 1 space.", and the ONE node HA-Q8 decides the position of.
        ///
        /// <para>The sentence is printed after the "End of turn:" sentence, and p.17 makes the text
        /// after a keyword that keyword's text, so it is composed here - Hanu's own optional move at
        /// the end of the turn, from wherever the swap back has just left him. The alternative reading
        /// is a move at resolution time, after the FIRST swap, which is the position Monkey Business
        /// and See Nothing print their identical sentence in; if the user takes that reading this one
        /// node moves into <see cref="Journey"/>'s sequence after
        /// <see cref="JourneyExchangeNode"/> and nothing else about the face changes.</para>
        /// </summary>
        public const string JourneyBackMoveNode = "journey-back-move";

        public const string JourneyBackActorAtOutput = "actorAt";

        public const string JourneyBackSubjectAtOutput = "subjectAt";

        public const string JourneyBackMovedOutput = "moved";

        /// <summary>
        /// The three faces' Skill body, as one factory. Nothing about it differs between the faces -
        /// the Radius is read off the card by the selection rather than declared here, and the
        /// trailing move belongs to the other body - so the only parameter that is not identity is
        /// which Active effect the face creates.
        /// </summary>
        public static EffectDefinition Journey(
            CardDefinitionId cardId,
            string contentRevision,
            EffectCapabilityReference journeyEffect)
        {
            var root = new EffectNodeId(JourneyRootNode);
            var subject = new EffectNodeId(JourneySubjectNode);
            var exchange = new EffectNodeId(JourneyExchangeNode);
            var effect = new EffectNodeId(JourneyEffectNode);

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, exchange, effect }),

                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            // p.9's Skill band: the card text IS the action, so the activity p.18's
                            // immunity is asked about is the Skill this text is printed on.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),

                            // "an ENEMY hero".
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),

                            // No face prints "not adjacent to you", so nothing is taken out of the
                            // disk's innermost ring.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),

                            // "with an enemy hero" can never be himself, and p.19 keeps self and
                            // another distinct in a Swap anyway.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(false)),

                            // Milestone 8.35l. The exchange decides whether a hero inside the disk is
                            // one this card can actually take - a hero some standing restriction bars
                            // from being swapped is greyed rather than offered and then refused.
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

                            // "a hero", which p.19 makes one of its two kinds of unit; the selection
                            // above can name nothing else, and this is the second statement of the
                            // same word.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                JourneyActorAtOutput, EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                JourneySubjectAtOutput, EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        effect, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        journeyEffect,
                        inputs: new[]
                        {
                            // "That hero" - the one the selection named, which is what makes the
                            // occurrence a Hero-scoped one rather than one about Hanu. It is bound
                            // from the SELECTION rather than from the exchange, because the exchange
                            // publishes spaces and the effect is about a hero: a space would have to
                            // be read back into an identity, and the board has moved between them.
                            EffectInputBinding.FromNodeOutput(
                                ActiveEffectCreationHandler.AffectedHeroParameterName, subject,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// What a journey runs when the turn ends: "Swap with that hero regardless of radius and
        /// immunity[. Move up to 1 space]."
        ///
        /// <para><paramref name="trailingMove"/> is Safe Travels and nothing else. Its move is
        /// <see cref="EffectFailurePolicy.UpTo"/> with the printed 1: p.11 makes a "move up to N
        /// spaces" a mandatory step whose DISTANCE is the decision, with zero admitted (Garrus's
        /// GA-Q1), which is the same reading Brynn's Hide Traces and Sabina's Steady Advance take of
        /// the same words. It starts from the space the exchange has just left him in rather than from
        /// the board asked again - the BR-Q20d binding - because p.19's Swap is neither movement nor
        /// placement, so nothing about it spends this budget.</para>
        /// </summary>
        public static EffectDefinition JourneyBack(
            EffectDefinitionId effectId, string contentRevision, bool trailingMove)
        {
            var root = new EffectNodeId(JourneyBackRootNode);
            var partner = new EffectNodeId(JourneyBackPartnerNode);
            var exchange = new EffectNodeId(JourneyBackExchangeNode);
            var move = new EffectNodeId(JourneyBackMoveNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: trailingMove
                        ? new[] { partner, exchange, move }
                        : new[] { partner, exchange }),

                new EffectNodeDefinition(
                    partner, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    HanuComponentContent.JourneyPartnerV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            HanuComponentContent.JourneyPartnerHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            HanuComponentContent.JourneyPartnerSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                new EffectNodeDefinition(
                    exchange, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", partner,
                            HanuComponentContent.JourneyPartnerSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            JourneyBackActorAtOutput, EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            JourneyBackSubjectAtOutput, EffectValueKind.Coordinate)
                    },

                    // "regardless of ... immunity" - and the hero it names is the one THIS card made
                    // immune three sentences earlier, so without the declaration the face would have
                    // spent a turn arranging a swap back it then refused itself.
                    immunityOverrides: new[]
                    {
                        new ImmunityOverrideDeclaration(
                            RuleActivity.Swap,
                            ImmunityOverrideReason.PrintedUnqualifiedImmunity)
                    })
            };

            if (trailingMove)
            {
                nodes.Add(new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", exchange, JourneyBackActorAtOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),

                        // The face says how far and nothing about where, so nothing is filtered by
                        // where anybody stands - p.11's own breadth.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            JourneyBackMovedOutput, EffectValueKind.Coordinate)
                    }));
            }

            return new EffectDefinition(effectId, contentRevision, root, nodes);
        }

        /// <summary>Unexpected Journey's end-of-turn body: the swap back, and nothing after it.</summary>
        public static class UnexpectedJourneyTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                JourneyBack(
                    new EffectDefinitionId("hanu-unexpected-journey-triggered"),
                    HanuCardContent.ContentRevision,
                    trailingMove: false);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>There and Back Again's, which prints the same three sentences.</summary>
        public static class ThereAndBackAgainTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                JourneyBack(
                    new EffectDefinitionId("hanu-there-and-back-again-triggered"),
                    HanuCardContent.ContentRevision,
                    trailingMove: false);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>Safe Travels': the swap back, then "Move up to 1 space." - HA-Q8.</summary>
        public static class SafeTravelsTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                JourneyBack(
                    new EffectDefinitionId("hanu-safe-travels-triggered"),
                    HanuCardContent.ContentRevision,
                    trailingMove: true);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>
        /// The three bodies a <c>TriggeredEffectCatalog</c> must hold for these faces to resolve - for
        /// the fixtures now and for the catalog 8.71h composes.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> JourneyTriggeredEffects() =>
            Array.AsReadOnly(new[]
            {
                UnexpectedJourneyTriggeredEffect.Definition,
                ThereAndBackAgainTriggeredEffect.Definition,
                SafeTravelsTriggeredEffect.Definition
            });

        /// <summary>
        /// NOTHING, and that is derived rather than assumed: a decision label answers a
        /// <c>Choose</c> arm or a node
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery.OpensConfirmOrDecline"/>
        /// names, and these three faces raise neither. Both bodies are sequences of mandatory
        /// capability nodes; Safe Travels' trailing move is
        /// <see cref="EffectFailurePolicy.UpTo"/>, which is a step whose distance is the decision
        /// rather than a confirm-or-decline - exactly as Brynn's Hide Traces and Silverarrow's whole
        /// set declare nothing for the same shape. <c>HanuJourneyTests</c> measures the requirement
        /// set with <see cref="DecisionLabelRequirementQuery"/> instead of trusting this sentence.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> JourneyDecisionLabels() =>
            Array.Empty<DecisionLabelDeclaration>();

        // MILESTONE 8.71g FOLDED `JourneySkillEffectId` AWAY into `HanuEffectContent.SkillEffectId`,
        // which mints the same `<card>-skill` string his other Skill bodies always carried.
    }
}
