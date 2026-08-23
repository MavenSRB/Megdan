using System;
using System.Collections.Generic;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.10. The one consequence rulebook p.13 gives a card's text a way to reach: a named
    /// hero is defeated.
    ///
    /// <para><b>It declares the Defeat and does not perform it.</b> That is deliberate, and it is the
    /// shape milestone 6.9's mid-turn Lane Push already uses: a capability publishes a typed fact, and
    /// the authority that owns the consequence reads it and runs the page. p.13's list prices a reward
    /// by Level, spends Life counters, returns markers, resolves an unresolved card and cancels Active
    /// effects - all of it authority state that no capability holds, and all of it already written once
    /// in the Attack resolver. A second copy inside a handler is exactly the disagreement milestone
    /// 6.6b closed between the hero and minion removal paths, so there is not one.</para>
    ///
    /// <para><b>Why it is a capability at all</b> rather than a branch in the resolver: Electroblast is
    /// not the last card that will print a defeat. High Voltage's composition lands at milestone 8.13
    /// and reads the same consequence, so the seam is registered content that any card can name.</para>
    /// </summary>
    public static class SyntheticConsequenceCapabilities
    {
        public static EffectCapabilityId DefeatHero { get; } =
            new EffectCapabilityId("synthetic-defeat-hero");

        public static EffectCapabilityReference DefeatHeroV1 =>
            new EffectCapabilityReference(DefeatHero, 1);

        public static EffectCapabilityId DefeatMinion { get; } =
            new EffectCapabilityId("synthetic-defeat-minion");

        public static EffectCapabilityReference DefeatMinionV1 =>
            new EffectCapabilityReference(DefeatMinion, 1);

        /// <summary>The parameter naming the hero rulebook p.13's list is run about.</summary>
        public const string HeroParameter = "hero";

        /// <summary>
        /// The declared output a defeating capability publishes its request on. One spelling, shared by
        /// the capability that writes it and the query that reads it, for the reason
        /// <c>RecordedLanePushQuery.OutcomeFactName</c> is shared.
        /// </summary>
        public const string DefeatedHeroOutput = "defeatedHero";

        public const string MinionParameter = "minion";

        public const string DefeatedMinionOutput = "defeatedMinion";

        /// <summary>
        /// Milestone 8.10. Whether the Attack this card text is printed inside took a hero, which is
        /// what Thunder Boomerang's "If you targeted a hero" asks before it offers its repeat.
        ///
        /// <para>It is a predicate over the enclosing action rather than over anything a node did, so
        /// it reads the context the resolver already carries - the same place
        /// <c>EnclosingActionKind</c> comes from - instead of requiring the Attack to publish a fact
        /// into a frame it does not own. p.13 lets an Attack take a minion, so "no" is an ordinary
        /// answer and not a missing one.</para>
        /// </summary>
        public static EffectCapabilityId AttackTookAHero { get; } =
            new EffectCapabilityId("synthetic-attack-took-a-hero");

        public static EffectCapabilityReference AttackTookAHeroV1 =>
            new EffectCapabilityReference(AttackTookAHero, 1);

        /// <summary>
        /// Whether the enclosing Attack took a hero. It is the Boolean a
        /// <see cref="EffectNodeKind.Conditional"/> branches on, which is why the answer is published
        /// rather than expressed as applying or declining: a predicate that declined would be saying
        /// the step could not happen, and "you took a minion" is an ordinary answer.
        /// </summary>
        public const string TookAHeroOutput = "tookAHero";

        /// <summary>
        /// Milestone 8.11. Whether the Attack this card text is answering is p.13's Ranged subtype,
        /// which is the Block condition all three of Wasp's Green cards print: "Block a ranged attack."
        ///
        /// <para>It is the same shape as <see cref="AttackTookAHero"/> and a second declaration rather
        /// than a parameter on it, for the reason the two printed-icon selections are two: these are
        /// two different questions about the enclosing Attack, and a card asks one of them. Deflect and
        /// Reflect ask it a second time for their riders, because "if you do" is the Block condition
        /// read again - p.14 puts the defender's text at step 4, before the totals, so the answer is
        /// already decided when the card is revealed.</para>
        /// </summary>
        public static EffectCapabilityId AttackIsRanged { get; } =
            new EffectCapabilityId("synthetic-attack-is-ranged");

        public static EffectCapabilityReference AttackIsRangedV1 =>
            new EffectCapabilityReference(AttackIsRanged, 1);

        /// <summary>Whether the enclosing Attack is Ranged; the Boolean a Conditional branches on.</summary>
        public const string IsRangedOutput = "isRanged";

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new DefeatHeroHandler();
            yield return new DefeatMinionHandler();
            yield return new AttackTookAHeroHandler();
            yield return new AttackIsRangedHandler();
        }

        private sealed class AttackIsRangedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AttackIsRangedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                Array.Empty<EffectCapabilityParameter>(),
                new[] { new EffectOutputBinding(IsRangedOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Always applies and publishes the answer. An Attack that is not Ranged is an ordinary
            /// Attack, not a step that could not be carried out.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        IsRangedOutput,
                        EffectValue.Boolean(
                            context.EnclosingAttack.Range == GuardsDigital.Core.Cards.AttackRangeKind.Ranged))
                });

            /// <summary>Unreachable: this capability opens no choice.</summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        private sealed class AttackTookAHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AttackTookAHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                Array.Empty<EffectCapabilityParameter>(),
                new[] { new EffectOutputBinding(TookAHeroOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Always applies, and publishes the answer. Reading it is the conditional's business, and
            /// an Attack that took a minion is an ordinary legal Attack rather than a step that could
            /// not be carried out.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        TookAHeroOutput,
                        EffectValue.Boolean(context.EnclosingAttack.HeroTargetId.IsDefined))
                });

            /// <summary>Unreachable: this capability opens no choice.</summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        private sealed class DefeatHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DefeatHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero) },
                new[] { new EffectOutputBinding(DefeatedHeroOutput, EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var hero = context.Input(HeroParameter).HeroId;
                if (!hero.IsDefined)
                {
                    // A step that names nobody cannot be carried out, and p.10 stops the enclosing
                    // sequence when a mandatory one cannot.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(DefeatedHeroOutput, EffectValue.Hero(hero))
                });
            }

            /// <summary>
            /// Unreachable: this capability opens no choice, so nothing ever resumes it. It is stated
            /// as a refusal rather than as a silent success, which is what every other handler with no
            /// choice does.
            /// </summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Input(HeroParameter).HeroId.IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class DefeatMinionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DefeatMinionV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(MinionParameter, EffectValueKind.Minion) },
                new[] { new EffectOutputBinding(DefeatedMinionOutput, EffectValueKind.Minion) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var minion = context.Input(MinionParameter).MinionId;
                return minion.IsDefined
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(
                            DefeatedMinionOutput, EffectValue.Minion(minion))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Input(MinionParameter).MinionId.IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
