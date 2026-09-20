using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's three tracking swaps - Tread Lightly, Cover Tracks and Hide Traces - which
    /// print one sentence between them and differ only in what follows it. Milestone 8.69d.
    ///
    /// <para><b>The sentence.</b> "Swap either with a unit adjacent to you, or with an enemy hero in
    /// radius who is adjacent to 3 or more obstacles." One selection over the union of two reaches
    /// (<see cref="BrynnComponentContent.TrackingSwapSubjectV1"/>), then p.19's exchange
    /// (<c>synthetic-swap-actor@1</c>), which is the same two-node shape every swapping face in this
    /// build already has.</para>
    ///
    /// <para><b>What follows it, and why the two trailing moves are two declarations.</b> Cover Tracks
    /// prints "You may move 1 space" and Hide Traces prints "Move up to 2 spaces". p.11 leaves the
    /// first exact and optional, which is <see cref="EffectFailurePolicy.May"/> - Tigerclaw's five "you
    /// may move 1 space" faces and Min's Tiger Stance exactly - and makes the second a mandatory step
    /// whose DISTANCE is the decision, which is <see cref="EffectFailurePolicy.UpTo"/> with zero
    /// admitted (Garrus's GA-Q1). Neither is a Movement action: p.19 and p.11 both make card-text
    /// movement its own thing, so no movement-action restriction bars it and no movement-action buff
    /// widens it.</para>
    ///
    /// <para><b>The move starts where the Swap left him</b> - the exchange publishes both spaces, and
    /// binding <c>actorAt</c> rather than asking the board again is Whisper's Cruel Twist reading of
    /// the same pair, and Arien's Ebb and Flow before it (BR-Q20d). p.19 makes the swap neither
    /// movement nor placement, so nothing about the exchange spends this budget and no "after
    /// movement" text and no place trigger is reached by it.</para>
    ///
    /// <para><b>An impossible swap stops the action, and the trailing move is never reached.</b> The
    /// selection and the exchange are both mandatory steps of one sequence, so a board on which no unit
    /// satisfies either reach is p.10's impossible mandatory step: the action performs nothing and the
    /// later sibling is never asked - not even Cover Tracks' "You may", because what p.10 stops is the
    /// ACTION rather than the one step. That is Rowenna's Defend the Innocent shape, stated by ordering
    /// rather than by a condition this definition invents.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        /// <summary>The selection node every one of the three faces roots its sequence on.</summary>
        public const string TrackingSwapRootNode = "tracking-swap-root";

        public const string TrackingSwapSubjectNode = "tracking-swap-subject";

        public const string TrackingSwapExchangeNode = "tracking-swap-exchange";

        /// <summary>The trailing move, present on the two faces that print one and on no other.</summary>
        public const string TrackingSwapMoveNode = "tracking-swap-move";

        /// <summary>The space the exchange leaves Brynn standing in.</summary>
        public const string TrackingSwapActorAtOutput = "actorAt";

        /// <summary>The space the exchange leaves his subject standing in.</summary>
        public const string TrackingSwapSubjectAtOutput = "subjectAt";

        public const string TrackingSwapMovedOutput = "moved";

        /// <summary>
        /// The three faces, as one factory over the one thing that differs: the trailing move.
        ///
        /// <para><paramref name="moveDistance"/> of zero is Tread Lightly, which prints none;
        /// <paramref name="moveIsUpTo"/> separates Hide Traces' mandatory "up to 2" from Cover Tracks'
        /// optional exact 1. One method and no overload of it, on milestone 8.57's own lesson: an
        /// overload re-points every IL rule that names the method.</para>
        /// </summary>
        public static EffectDefinition TrackingSwap(
            CardDefinitionId cardId, string contentRevision, int moveDistance, bool moveIsUpTo)
        {
            var root = new EffectNodeId(TrackingSwapRootNode);
            var subject = new EffectNodeId(TrackingSwapSubjectNode);
            var exchange = new EffectNodeId(TrackingSwapExchangeNode);
            var move = new EffectNodeId(TrackingSwapMoveNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: moveDistance > 0
                        ? new[] { subject, exchange, move }
                        : new[] { subject, exchange }),

                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    BrynnComponentContent.TrackingSwapSubjectV1,
                    outputs: new[]
                    {
                        // All three, because the selection publishes whichever kind of unit was
                        // chosen and an undeclared recorded outcome is a snapshot this build refuses
                        // to read back - Whisper's own union node declares the same three.
                        new EffectOutputBinding(
                            BrynnComponentContent.ChosenSpaceOutputName,
                            EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            BrynnComponentContent.ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            BrynnComponentContent.ChosenMinionOutputName, EffectValueKind.Minion)
                    }),

                new EffectNodeDefinition(
                    exchange, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", subject,
                            BrynnComponentContent.ChosenSpaceOutputName,
                            EffectValueKind.Coordinate),

                        // "a unit", which p.19 makes a hero or a minion and never a token.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            TrackingSwapActorAtOutput, EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            TrackingSwapSubjectAtOutput, EffectValueKind.Coordinate)
                    })
            };

            if (moveDistance > 0)
            {
                nodes.Add(new EffectNodeDefinition(
                    move,
                    EffectNodeKind.Capability,
                    moveIsUpTo ? EffectFailurePolicy.UpTo : EffectFailurePolicy.May,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", exchange, TrackingSwapActorAtOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral(
                            "distance", EffectValue.Integer(moveDistance)),

                        // Neither face names where he may go, so neither declares a policy narrower
                        // than p.11's own.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(TrackingSwapMovedOutput, EffectValueKind.Coordinate)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }
    }
}
