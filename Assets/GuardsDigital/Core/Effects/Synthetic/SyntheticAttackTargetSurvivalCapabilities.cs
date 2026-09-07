using System;
using System.Collections.Generic;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z50's <c>attack-target-survived@1</c>: whether rulebook p.14 step 6
    /// removed the enclosing Attack's own target.
    ///
    /// <para>Ursafar's Prey Drive, Prey Abundance and Feeding Frenzy each print "After the attack: If
    /// enraged, and the target was not removed, remove up to N enemy minion(s) in radius." -
    /// <c>UrsafarEffectContent.RemovalGatedOnTargetSurvival</c> names the three faces this closes -
    /// phase 1 of this hero named the missing fact as a shortfall
    /// because nothing in the engine recorded whether an Attack's target survived. It does now:
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.TargetWasRemoved"/> is
    /// read live off board occupancy at the moment card text can ask the question, so this handler is
    /// a thin publisher over a fact the resolution authority already derives - it stores nothing of
    /// its own, costs no snapshot bytes and moves no schema.</para>
    ///
    /// <para><b>Published as "survived", matching the printed phrase.</b> The card says "the target was
    /// <em>not</em> removed", and every conditional gate in this codebase branches on a plain Boolean
    /// with no negation of its own (<c>IsFlaggedV1</c> reads a flag and republishes it) - so this
    /// answers exactly the question the card asks, and no node anywhere has to invert it.</para>
    ///
    /// <para><b>Deferred, not registered</b>, for the reason
    /// <see cref="SyntheticConditionCapabilities"/> records: the registered set is serialized, and the
    /// only content naming this is a hero this build does not deal.</para>
    /// </summary>
    public static class SyntheticAttackTargetSurvivalCapabilities
    {
        public static EffectCapabilityId AttackTargetSurvived { get; } =
            new EffectCapabilityId("attack-target-survived");

        public static EffectCapabilityReference AttackTargetSurvivedV1 =>
            new EffectCapabilityReference(AttackTargetSurvived, 1);

        /// <summary>Whether the enclosing Attack's own target is still on the board.</summary>
        public const string TargetSurvivedOutput = "targetSurvived";

        /// <summary>
        /// Milestone 8.35z50 registered this handler beside the hero whose dealt cards start it. Unlike
        /// its two neighbours in this dealing it is Ursafar-only - his three removal Reds are the whole
        /// consumer set - so no shared-sharer check applies to it.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[] { new AttackTargetSurvivedHandler() };

        /// <summary>
        /// Nothing is deferred here any longer; the member survives for the offline content tests that
        /// compose catalogs of their own from the deferred lists.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        private sealed class AttackTargetSurvivedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AttackTargetSurvivedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(TargetSurvivedOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Always applies and publishes the answer. Whether the target survived is an ordinary
            /// fact about the Attack, never a step that could not be carried out.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        TargetSurvivedOutput,
                        EffectValue.Boolean(!context.EnclosingAttack.TargetWasRemoved))
                });

            /// <summary>Unreachable: this capability opens no choice.</summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
