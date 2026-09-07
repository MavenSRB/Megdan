using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
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

        /// <summary>
        /// Milestone 8.35z45. The predicate this bullet prints about the candidate, as
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.AttackSequenceSubjectNarrowing"/>.
        ///
        /// <para><b>Optional, and the absence is the absence of the clause.</b> Every bullet composed
        /// before Middlefinger of Death - Whisper's four - declares nothing here and is unchanged down
        /// to the byte: no definition it names moved, no content revision of hers moved, and the plans
        /// her card produces carry <see cref="AttackSequenceSubjectNarrowing.None"/> exactly as they did
        /// when the field did not exist. That is the same shape milestone 8.35z28's two hero narrowings
        /// take on the printed-icon selection, and it is what lets a widening of a persisted type leave
        /// every earlier consumer alone.</para>
        ///
        /// <para>It is named <c>subjectNarrowing</c> rather than anything ending in a quantity word for
        /// the reason <see cref="ReachParameter"/> is not called "reach": content load keeps a closed
        /// list of ordinary quantities, a parameter named from it may not declare an enum domain, and
        /// one outside it must. This is a closed vocabulary of printed clauses, so it takes a name that
        /// says so and declares its domain.</para>
        /// </summary>
        public const string SubjectNarrowingParameter = "subjectNarrowing";

        public const string TargetableUnitsOutput = "attackSequenceUnits";
        public const string ReachOutput = "attackSequenceReach";
        public const string LineOutput = "attackSequenceLine";
        public const string DistinctTargetsOutput = "attackSequenceDistinctTargets";

        /// <summary>
        /// Milestone 8.35z45. The chosen bullet's printed predicate, published so the plan
        /// the frame names carries it into the sequence that will apply it.
        /// </summary>
        public const string SubjectNarrowingOutput = "attackSequenceSubjectNarrowing";

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
                            DistinctTargetsParameter, EffectValueKind.Boolean, true),
                        new EffectCapabilityParameter(
                            SubjectNarrowingParameter,
                            EffectValueKind.Integer,
                            isRequired: false,
                            integerDomain: EffectIntegerDomain.All<AttackSequenceSubjectNarrowing>())
                    },
                    new[]
                    {
                        new EffectOutputBinding(TargetableUnitsOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(ReachOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(LineOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(DistinctTargetsOutput, EffectValueKind.Boolean),
                        new EffectOutputBinding(SubjectNarrowingOutput, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!IsAReadableBullet(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(
                        TargetableUnitsOutput, context.Input(TargetableUnitsParameter)),
                    new EffectOutcomeFact(ReachOutput, context.Input(ReachParameter)),
                    new EffectOutcomeFact(LineOutput, context.Input(LineParameter)),
                    new EffectOutcomeFact(
                        DistinctTargetsOutput, context.Input(DistinctTargetsParameter))
                };

                // Milestone 8.35z45. Published only by a bullet that prints the clause. A
                // bullet declaring no narrowing emits no fact, so every outcome this capability has
                // ever produced - and therefore every byte a snapshot writes for one - is unchanged;
                // the reader takes the absent fact as the absent clause, exactly as it takes an
                // unbound optional parameter.
                var narrowing = DeclaredNarrowing(context);
                if (narrowing != AttackSequenceSubjectNarrowing.None)
                {
                    facts.Add(new EffectOutcomeFact(
                        SubjectNarrowingOutput, EffectValue.Integer((int)narrowing)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            /// <summary>
            /// The predicate this bullet's node declares, or
            /// <see cref="AttackSequenceSubjectNarrowing.None"/> where it declares nothing - which is
            /// every bullet composed before Middlefinger of Death.
            /// </summary>
            private static AttackSequenceSubjectNarrowing DeclaredNarrowing(
                EffectCapabilityContext context)
            {
                var declared = context.Input(SubjectNarrowingParameter);
                return declared.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(typeof(AttackSequenceSubjectNarrowing), declared.IntegerValue)
                        ? (AttackSequenceSubjectNarrowing)declared.IntegerValue
                        : AttackSequenceSubjectNarrowing.None;
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
