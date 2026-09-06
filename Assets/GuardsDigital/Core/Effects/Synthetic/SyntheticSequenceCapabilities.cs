using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z27. The one capability a modal Attack's "choose one, or both" needs:
    /// a leaf under an ordinary <see cref="EffectNodeKind.Choose"/> that publishes "this bullet was
    /// chosen, and here is the sentence it targets by".
    ///
    /// <para><b>It performs nothing.</b> The transport is milestone 6.9's Lane-Push cause, milestone
    /// 8.10's Defeat request and milestone 8.19a's <c>around-primary-action@1</c>: the node publishes a
    /// typed fact, the authority that owns the action reads it, and rulebook p.14 is run by the Attack
    /// resolver exactly as it is for every other Attack in the build.</para>
    ///
    /// <para><b>Why it is not <c>set-attack-target@1</c>.</b> That capability names a unit, and a
    /// modal Attack must not name one here: Ruling 5 runs a complete p.14 per chosen bullet, and each
    /// bullet binds its target when its own sequence starts. A bullet chosen at the modal and bound at
    /// the modal would have chosen from a board one whole Attack too early. So what crosses is the
    /// bullet's <em>sentence</em> - which units, how far, and whether p.19's straight line is excluded
    /// - and the sequence derives its candidates from that against the board it actually starts on.
    /// The predicate half of a bullet a policy cannot state (Middlefinger's "a hero in range who has
    /// one or more cards in the discard") is named in
    /// <see cref="GuardsDigital.Core.Rules.CardResolution.AttackSequenceBullet"/>'s documentation as
    /// the consumer milestone's extension point; nothing in this build prints one.</para>
    ///
    /// <para><b>Why it is not a Repeat.</b> See
    /// <see cref="GuardsDigital.Core.Rules.CardResolution.AttackSequencePlan"/>. The label is the
    /// whole point of the split.</para>
    /// </summary>
    public static class SyntheticSequenceCapabilities
    {
        /// <summary>`attack-sequence@1`.</summary>
        public static EffectCapabilityId AttackSequence { get; } =
            new EffectCapabilityId("attack-sequence");

        public static EffectCapabilityReference AttackSequenceV1 =>
            new EffectCapabilityReference(AttackSequence, 1);

        /// <summary>Which units this bullet's own sentence lets its sequence take.</summary>
        public const string TargetableUnitsParameter = "units";

        /// <summary>
        /// How far this bullet's own sentence reaches, as
        /// <see cref="AttackTargetReach"/> rather than as a number.
        ///
        /// <para>It is deliberately NOT called "reach". That word is on content load's closed list of
        /// ordinary quantities - counts, distances, radii, ranges and amounts - and a parameter named
        /// from that list is forbidden to declare an enum domain, exactly as one outside it is
        /// required to. This is a closed vocabulary of two printed readings ("adjacent units only" and
        /// "the printed Range"), not a distance, so it takes a name that says which of the two rules
        /// it is under.</para>
        /// </summary>
        public const string ReachParameter = "targetReach";

        /// <summary>Whether this bullet's own sentence excludes rulebook p.19's straight line.</summary>
        public const string LineParameter = "line";

        /// <summary>Whether the card printed "(on different targets)" across its bullets.</summary>
        public const string DistinctTargetsParameter = "distinctTargets";

        public const string TargetableUnitsOutput = "attackSequenceUnits";
        public const string ReachOutput = "attackSequenceReach";
        public const string LineOutput = "attackSequenceLine";
        public const string DistinctTargetsOutput = "attackSequenceDistinctTargets";

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new AttackSequenceHandler();
        }

        /// <summary>
        /// The bullet id a node stands for: the node's own stable content identity.
        ///
        /// <para>There is no printed-ordinal parameter, and that is deliberate. A bullet already has
        /// exactly one identity in this engine - the effect node the card declares it as - and the
        /// order the bullets run in is the order their outcomes arrive in. A second ordinal carried as
        /// a literal would be a second place the same fact could be wrong, which is the rule the whole
        /// of this file follows; it would also be an integer parameter that is neither an ordinary
        /// quantity nor an enum-backed vocabulary, which content load refuses outright.</para>
        /// </summary>
        public static string BulletIdFor(EffectNodeId nodeId) => nodeId.Value;

        private sealed class AttackSequenceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AttackSequenceV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            TargetableUnitsParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<AttackTargetableUnits>()),
                        new EffectCapabilityParameter(
                            ReachParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<AttackTargetReach>()),
                        new EffectCapabilityParameter(
                            LineParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<AttackTargetLine>()),
                        new EffectCapabilityParameter(
                            DistinctTargetsParameter, EffectValueKind.Boolean, true)
                    },
                    new[]
                    {
                        new EffectOutputBinding(TargetableUnitsOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(ReachOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(LineOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(DistinctTargetsOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!IsAReadableBullet(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        TargetableUnitsOutput, context.Input(TargetableUnitsParameter)),
                    new EffectOutcomeFact(ReachOutput, context.Input(ReachParameter)),
                    new EffectOutcomeFact(LineOutput, context.Input(LineParameter)),
                    new EffectOutcomeFact(
                        DistinctTargetsOutput, context.Input(DistinctTargetsParameter))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            /// <summary>
            /// Choosing a bullet is always possible; whether that bullet's sequence will find a unit
            /// is p.14's question, asked when the sequence starts and answered against the board it
            /// starts on. Anticipating otherwise would be milestone 8.35z15's mistake inverted -
            /// refusing a mode on a board reading that the sequence has not reached yet.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                IsAReadableBullet(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static bool IsAReadableBullet(EffectCapabilityContext context)
            {
                var units = context.Input(TargetableUnitsParameter);
                var reach = context.Input(ReachParameter);
                var line = context.Input(LineParameter);
                var distinct = context.Input(DistinctTargetsParameter);
                return units.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(typeof(AttackTargetableUnits), units.IntegerValue) &&
                    reach.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(typeof(AttackTargetReach), reach.IntegerValue) &&
                    line.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(typeof(AttackTargetLine), line.IntegerValue) &&
                    distinct.Kind == EffectValueKind.Boolean;
            }
        }
    }
}
