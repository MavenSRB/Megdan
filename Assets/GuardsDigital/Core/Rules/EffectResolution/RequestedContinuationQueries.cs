using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.19a. The unit a completed card-text frame named as its enclosing Attack's target,
    /// read exactly the way milestone 8.10 reads a requested Defeat and milestone 6.9 reads p.8's
    /// Lane-Push cause.
    ///
    /// <para>A request is an ordinary applied node outcome. That transport is what makes the binding
    /// survive a pause, a snapshot restore, a replay and a retained-prefix Undo without a field of its
    /// own anywhere: the frame already carries its completed outcomes, and one of them says which unit
    /// the text chose. Brogan's Mad Dash pauses twice before it names anything - once to pick the
    /// charge line and once for the move it performs - so the binding has to be a fact rather than a
    /// value someone held.</para>
    ///
    /// <para>The node is confirmed against the trusted definition before its fact is believed, which is
    /// milestone 6.7's discipline: a payload names node identities and never semantics, so an outcome
    /// naming a node this definition does not declare names nothing.</para>
    /// </summary>
    public static class RequestedAttackTargetQuery
    {
        /// <summary>
        /// The target the frame's completed outcomes bound, or the undefined target when they bound
        /// none. The last binding wins, because a text that binds twice has re-aimed the same Attack
        /// and p.13 gives it one target.
        /// </summary>
        public static AttackTarget Bound(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return default;
            }

            var bound = default(AttackTarget);
            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                {
                    continue;
                }

                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Hero && fact.Value.HeroId.IsDefined)
                    {
                        bound = AttackTarget.Hero(fact.Value.HeroId);
                    }
                    else if (string.Equals(
                            fact.OutputName,
                            SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Minion && fact.Value.MinionId.IsDefined)
                    {
                        bound = AttackTarget.Minion(fact.Value.MinionId);
                    }
                }
            }

            return bound;
        }
    }

    /// <summary>
    /// Milestone 8.19a. Which side of its primary action a completed ordering frame placed the rest of
    /// its text on. Same transport, same confirmation against the trusted definition, and the same
    /// reason: the answer has to survive whatever the action does between the question and the work.
    /// </summary>
    /// <summary>
    /// Milestone 8.35z29. Whether a completed effect placed the hero performing it - the trigger
    /// Misa's Power Overwhelming watches: "Each time after you place yourself, an enemy hero adjacent
    /// to you discards a card, if able."
    ///
    /// <para><b>The question is answered from the definition, not from a name or a fact anybody had to
    /// remember to publish.</b> A Place node states which piece it moves through its <c>subject</c>
    /// input, and the one way a definition can name the acting hero as that subject is
    /// <c>synthetic-actor-space@1</c> - the bridge milestone 6.5a built precisely because no role can
    /// supply a coordinate. So "she placed herself" is exactly "an applied Place whose subject came
    /// from the actor-space bridge", which is a structural fact about the trusted definition rather
    /// than an assertion a card could make about itself. It is the same discriminator the 2026-09-06
    /// defense-offer corrective settled on when it had to tell a card's own self-move from a move it
    /// performs on somebody else.</para>
    ///
    /// <para><b>What it deliberately does not answer.</b> p.18 makes a Swap "not movement or
    /// placement", and no Swap capability is named here, so neither of Misa's Swap cards can reach the
    /// trigger - which is the user's 2026-09-06 ruling. Nor can a placement performed by another
    /// hero's card: this walks the definition the acting hero is resolving, and a placement made by
    /// somebody else's card is that hero's effect, not hers. That is the same ruling's other half -
    /// "you place yourself" names her as the actor.</para>
    /// </summary>
    public static class SelfPlacementQuery
    {
        public static bool Placed(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return false;
            }

            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out var node) ||
                    node.Kind != EffectNodeKind.Capability ||
                    !IsPlace(node.Capability) ||
                    !SubjectIsTheActor(definition, node))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsPlace(EffectCapabilityReference capability) =>
            capability.Equals(SyntheticPlacementCapabilities.PlaceV2) ||
            capability.Equals(SyntheticPlacementCapabilities.PlaceInRangeV1) ||
            capability.Equals(SyntheticPlacementCapabilities.PlaceInRadiusV1) ||
            capability.Equals(SyntheticPlacementCapabilities.PlaceAtMaximumRangeV1) ||
            capability.Equals(SyntheticPlacementCapabilities.PlaceInRadiusBesideTwoOrMoreEnemiesV1);

        private static bool SubjectIsTheActor(
            EffectDefinition definition,
            EffectNodeDefinition place)
        {
            foreach (var input in place.Inputs)
            {
                if (!string.Equals(input.ParameterName, "subject", StringComparison.Ordinal) ||
                    !input.SourceNodeId.IsDefined ||
                    !definition.TryGetNode(input.SourceNodeId, out var source))
                {
                    continue;
                }

                return source.Kind == EffectNodeKind.Capability &&
                    source.Capability.Equals(SyntheticPlacementCapabilities.ActorSpaceV1);
            }

            return false;
        }
    }

    public static class AroundActionPlacementQuery
    {
        public static PreActionPlacement Placed(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return PreActionPlacement.Undecided;
            }

            var placement = PreActionPlacement.Undecided;
            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                {
                    continue;
                }

                foreach (var fact in outcome.Outputs)
                {
                    if (!string.Equals(
                            fact.OutputName,
                            SyntheticContinuationCapabilities.PlacementOutput,
                            StringComparison.Ordinal) ||
                        fact.Value.Kind != EffectValueKind.Integer)
                    {
                        continue;
                    }

                    if (fact.Value.IntegerValue ==
                        SyntheticContinuationCapabilities.PlacementBeforeTheAction)
                    {
                        placement = PreActionPlacement.BeforeTheAction;
                    }
                    else if (fact.Value.IntegerValue ==
                        SyntheticContinuationCapabilities.PlacementAfterTheAction)
                    {
                        placement = PreActionPlacement.AfterTheAction;
                    }
                }
            }

            return placement;
        }
    }
}
