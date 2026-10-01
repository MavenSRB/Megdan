using System;
using System.Collections.Generic;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.30g. A team victory a printed card counts toward and then claims - Gydion's Wish:
    /// "Cast any spell in the Spellbook. After you cast the "Wish" spell three times your team wins the
    /// game." Two steps, both reaching the round through <see cref="EffectRosterState.TeamVictory"/>:
    /// <list type="bullet">
    /// <item><see cref="AdvanceTeamVictoryTallyV1"/> - one more for the acting hero's team, toward the
    /// card's printed <see cref="ThresholdParameter"/> (bound at the call site, as Cutter's thirteen
    /// is). Wish composes it first: its primary is performed exactly when it is cast, so the count rises
    /// when Wish is cast (GY-Q10).</item>
    /// <item><see cref="ClaimTeamVictoryV1"/> - records the acting hero's team as the winner once its
    /// tally has reached its threshold. Wish composes it LAST, after the inner cast: a cast spell's
    /// action runs inside the casting text and the casting text resumes after it (8.35z50's borrow
    /// path), so the claim is taken once the third Wish's whole action, its inner spell included, has
    /// resolved. It records and does not end; an ending the inner spell already caused is left
    /// standing by the round's own refusal.</item>
    /// </list>
    ///
    /// <para><b>"You" is the hero casting the spell</b> - the frame's actor - because Prepare Spells
    /// prints "The action on the spell card is performed by the hero casting the spell".</para>
    ///
    /// <para><b>Registered with Gydion's dealing (9.30i)</b>, whose dealt Prepare Spells reaches Wish
    /// once The Archwizard is unlocked; until then both were deferred, because the registered set is
    /// serialized into every snapshot.</para>
    /// </summary>
    public static class SyntheticTeamVictoryCapabilities
    {
        public static EffectCapabilityReference AdvanceTeamVictoryTallyV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("advance-team-victory-tally"), 1);

        public static EffectCapabilityReference ClaimTeamVictoryV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("claim-team-victory"), 1);

        /// <summary>The printed number of times the count must reach.</summary>
        public const string ThresholdParameter = "threshold";

        /// <summary>The team's count after this step.</summary>
        public const string CountOutput = "count";

        /// <summary>Whether this resolution claimed the match.</summary>
        public const string ClaimedOutput = "claimed";

        /// <summary>The count and the claim, registered with Gydion's dealing (9.30i).</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[] { new AdvanceHandler(), new ClaimHandler() };

        /// <summary>Nothing since Gydion's dealing (9.30i) registered both.</summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        private static bool CanAsk(EffectCapabilityContext context) =>
            context.RosterState.IsAvailable &&
            context.RosterState.TeamVictory != null &&
            context.ActorId.IsDefined;

        private sealed class AdvanceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AdvanceTeamVictoryTallyV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(ThresholdParameter, EffectValueKind.Integer) },
                new[] { new EffectOutputBinding(CountOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool CanApply(EffectCapabilityContext context) =>
                CanAsk(context) && context.Input(ThresholdParameter).IntegerValue > 0;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanApply(context) ||
                    !context.RosterState.TeamVictory.TryAdvanceTeamVictoryTally(
                        context.ActorId, context.Input(ThresholdParameter).IntegerValue))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var count = 0;
                var tally = GuardsDigital.Core.Rules.Victory.TeamVictoryTallyQuery.ForHero(
                    context.Participants, context.RosterState.TeamVictory.TeamVictoryTallies, context.ActorId);
                if (tally.HasValue) count = tally.Value.Count;
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(CountOutput, EffectValue.Integer(count))
                });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>A walk judging the text must not count, so this only asks whether it could.</summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanApply(context) ? EffectExecutionResultKind.Applied : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class ClaimHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ClaimTeamVictoryV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(ClaimedOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Short of the threshold the step applies and claims nothing - "after you cast it three
            /// times" is a condition on a clause every Wish reaches - so the published boolean is the
            /// whole answer and no Conditional is needed in front of it.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanAsk(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var claimed = context.RosterState.TeamVictory.TryClaimTeamVictory(context.ActorId);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ClaimedOutput, EffectValue.Boolean(claimed))
                });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>A walk judging the branch must not take it.</summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanAsk(context) ? EffectExecutionResultKind.Applied : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
