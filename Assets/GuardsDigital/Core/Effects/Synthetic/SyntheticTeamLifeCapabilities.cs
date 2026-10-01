using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.30f. The first capability that RAISES a team's Life counters - Gydion's Energy
    /// Drain, "Your team regains 1 spent [Life] Life counter."
    ///
    /// <para><b>"Your team" is the acting hero's.</b> The caster performs the spell (GY-Q3), so the team
    /// is the one the match places the actor on - read through <c>MatchParticipants</c>, never a side
    /// written into the card.</para>
    ///
    /// <para><b>"Spent" is a printed word with a rule behind it (GY-Q11, user, 2026-10-01).</b> The team
    /// regains a counter only when it has spent one, so never above the Lives it began with, and
    /// whether or not the enemy hero of the same card discarded: the two sentences are separate steps.
    /// <c>TeamLifeState.TryRegainSpent</c> owns the arithmetic; this step only names whose team and how
    /// many. With nothing spent it has nothing to do, which is reported as a declined step rather than
    /// a failure, so the card's <c>IfAble</c> skips it silently.</para>
    ///
    /// <para><b>Registered with Gydion's dealing (9.30i)</b>, whose dealt Greater Necromancy casts
    /// Energy Drain; until then it was deferred, because registering moves the capability list every
    /// snapshot pins.</para>
    /// </summary>
    public static class SyntheticTeamLifeCapabilities
    {
        public static EffectCapabilityId RegainSpentLife { get; } =
            new EffectCapabilityId("regain-spent-life-counter");

        public static EffectCapabilityReference RegainSpentLifeV1 =>
            new EffectCapabilityReference(RegainSpentLife, 1);

        /// <summary>How many spent counters the sentence regains - Energy Drain prints one.</summary>
        public const string AmountParameter = "amount";

        /// <summary>The counters actually regained, published only when the regain happened.</summary>
        public const string RegainedOutput = "regained";

        /// <summary>The handlers this file declares, registered with Gydion's dealing (9.30i).</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[] { new RegainSpentLifeHandler() };

        /// <summary>Nothing since Gydion's dealing (9.30i) registered the regain.</summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        private sealed class RegainSpentLifeHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RegainSpentLifeV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(AmountParameter, EffectValueKind.Integer) },
                    new[] { new EffectOutputBinding(RegainedOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryFindSpent(context, out var lives, out var team, out var amount))
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

                var regain = lives.TryRegainSpent(team, amount);
                return regain.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(RegainedOutput, EffectValue.Integer(regain.Regain.Regained))
                    })
                    : EffectExecutionResult.DeclinedOptional();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A Life regain never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryFindSpent(context, out _, out _, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.DeclinedOptional;

            /// <summary>
            /// The live Life state, the acting hero's team and the amount, when that team has a spent
            /// counter to take back. A composition with no Life state, no participants, an actor the
            /// match does not field or a non-positive amount has nothing to regain.
            /// </summary>
            private static bool TryFindSpent(
                EffectCapabilityContext context,
                out GuardsDigital.Core.Rules.Consequences.TeamLifeState lives,
                out GuardsDigital.Core.Participants.TeamId team,
                out int amount)
            {
                lives = context?.RosterState != null && context.RosterState.IsAvailable
                    ? context.RosterState.TeamLives
                    : null;
                team = default;
                amount = context?.Input(AmountParameter).IntegerValue ?? 0;
                var participants = context?.Participants;
                if (lives == null || participants == null || amount < 1 ||
                    !participants.Heroes.Any(hero => hero.Id.Equals(context.ActorId)))
                {
                    return false;
                }

                team = participants.GetTeamForHero(context.ActorId).Id;
                return lives.GetSpentCounters(team) > 0 && lives.GetCounters(team) > 0;
            }
        }
    }
}
