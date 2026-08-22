using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.10: which heroes a resolution's completed outcomes asked rulebook p.13's Defeat list
    /// to be run about, read exactly the way milestone 6.9 reads p.8's Lane-Push cause.
    ///
    /// <para>A request is an ordinary applied node outcome, which is the transport milestone 6.7
    /// established and 6.9 reused: a fact in the completed frame survives a pause, a snapshot restore,
    /// a replay and a retained-prefix Undo without a field of its own inside the effect frame. Nothing
    /// new is persisted to make a defeat requested inside card text survive the question that card text
    /// stopped on - and Electroblast's rider stops on two.</para>
    ///
    /// <para>The node is confirmed against the trusted definition before its fact is believed, which is
    /// 6.7's discipline: a payload names node identities and never semantics, so an outcome naming a
    /// node this definition does not declare asks for nothing.</para>
    /// </summary>
    public static class RequestedHeroDefeatQuery
    {
        /// <summary>
        /// The heroes named by every applied defeat request in <paramref name="outcomes"/>, in the
        /// order the frame recorded them, so a composition asking twice is run twice in its own order
        /// rather than collapsed to one answer this query picked.
        /// </summary>
        public static IReadOnlyList<HeroId> Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return Array.Empty<HeroId>();
            }

            var requested = new List<HeroId>();
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
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Hero &&
                        fact.Value.HeroId.IsDefined)
                    {
                        requested.Add(fact.Value.HeroId);
                    }
                }
            }

            return requested;
        }
    }
}
