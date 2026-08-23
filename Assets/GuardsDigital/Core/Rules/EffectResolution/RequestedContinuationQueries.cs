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
