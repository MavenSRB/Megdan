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
        /// Milestone 8.57a. WHO the enclosing Attack's target is, published only when that target is a
        /// hero - Rowenna's Code of Chivalry, "If you target a hero, both you and <b>the target</b> may
        /// retrieve a discarded card".
        ///
        /// <para><b>It is the same fact as <see cref="TookAHeroOutput"/> and not a second reading of
        /// it.</b> Both are <c>EnclosingAttackFacts.HeroTargetId</c>: one says whether it is defined and
        /// this one says which hero it is. Splitting them across two capabilities would be two answers
        /// to one question, which is exactly what this catalog's rule 1 refuses; adding an output is
        /// what milestone 8.40i did to <c>retrieve-card@1</c> for the identical reason, and its remarks
        /// record that an added OUTPUT moves no corpus digest, because the identity walker digests a
        /// capability's reference and its Active-effect definitions and never its parameter
        /// schema.</para>
        ///
        /// <para><b>Why the predicate had to carry it rather than the branch reading it.</b> Milestone
        /// 8.40c's rule is that a <see cref="EffectNodeKind.Conditional"/>'s branch cannot bind that
        /// Conditional's own output - so a card whose "if" is about the target and whose consequence is
        /// ALSO about the target needs the fact published by an earlier sibling. Code of Chivalry is
        /// the first face in the corpus that names the Attack's target as the SUBJECT of a step rather
        /// than only as the thing the Attack happened to, so the sibling is this capability asked
        /// plainly, and the Conditional over <c>synthetic-is-flagged@1</c> reads its Boolean - Bain's
        /// Drinking Buddies' exact shape.</para>
        ///
        /// <para><b>Published only for a node that declared it</b>, and only when the target is a hero,
        /// so Thunder Boomerang's shipped conditional is byte-identical without naming it and a minion
        /// target leaves the binding undefined - which is the answer a step naming "the target" needs,
        /// because p.13 lets an Attack take a minion and a minion has no hand to retrieve into.</para>
        /// </summary>
        public const string AttackTargetHeroOutput = "attackTargetHero";

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
                new[]
                {
                    new EffectOutputBinding(TookAHeroOutput, EffectValueKind.Boolean),

                    // Milestone 8.57a. The same fact as an identity, for a card whose consequence is
                    // about the target - see AttackTargetHeroOutput.
                    new EffectOutputBinding(AttackTargetHeroOutput, EffectValueKind.Hero)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Always applies, and publishes the answer. Reading it is the conditional's business, and
            /// an Attack that took a minion is an ordinary legal Attack rather than a step that could
            /// not be carried out.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var target = context.EnclosingAttack.HeroTargetId;
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(TookAHeroOutput, EffectValue.Boolean(target.IsDefined))
                };

                // Only for a node that declared it, and only where there is a hero to name: a minion
                // target leaves the binding undefined, which is what a step about "the target" needs.
                if (target.IsDefined && context.Publishes(AttackTargetHeroOutput))
                {
                    facts.Add(new EffectOutcomeFact(
                        AttackTargetHeroOutput, EffectValue.Hero(target)));
                }

                return EffectExecutionResult.Applied(facts);
            }

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
