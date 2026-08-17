using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.BoardObjects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 6.9: whether a resolution's completed outcomes contain rulebook p.8's recorded
    /// Lane-Push cause, read the way milestone 6.7 reads a repeat request.
    ///
    /// A cause is already an ordinary applied node outcome - the removing capability publishes it as a
    /// typed fact in the frame where a later node could bind it - and 6.7 established why that is the
    /// transport worth reusing: a fact in the completed frame survives a pause, a snapshot restore, a
    /// replay and a retained-prefix Undo without a field of its own inside the effect frame. Nothing new
    /// is stored to make a mid-turn push survive a paused Skill.
    /// </summary>
    public static class RecordedLanePushQuery
    {
        /// <summary>
        /// The declared output a removing capability publishes its p.8 answer on. One spelling, shared
        /// by the capability that writes it and the query that reads it, for the reason
        /// <c>SyntheticExchangeCapabilities.EligiblePieceParameterName</c> is shared.
        /// </summary>
        public const string OutcomeFactName = "lanePush";

        /// <summary>
        /// Which of p.8's two sentences this completed resolution answered, or the named absence.
        ///
        /// The answer is <see cref="LanePushCauseKind.Removal"/> and never <c>Defeat</c>, because an
        /// effect definition reaches p.8 through p.19's standalone Remove alone: that is the one
        /// capability declaring a removal stage, and it declares <see cref="LanePushCauseKind.Removal"/>
        /// as its cause. p.13's Defeat is not an effect node at all - it is the Attack resolver's own
        /// stage - and it carries its cause on <c>MinionDefeatResolution</c> rather than through a frame.
        ///
        /// The node is confirmed against the trusted definition before its fact is believed, which is
        /// 6.7's discipline: a payload names node identities and never semantics, so an outcome naming
        /// a node this definition does not declare answers nothing.
        /// </summary>
        public static LanePushCauseKind Recorded(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return LanePushCauseKind.None;
            }

            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                {
                    continue;
                }

                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(fact.OutputName, OutcomeFactName, StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Boolean && fact.Value.BooleanValue)
                    {
                        return LanePushCauseKind.Removal;
                    }
                }
            }

            return LanePushCauseKind.None;
        }
    }
}
