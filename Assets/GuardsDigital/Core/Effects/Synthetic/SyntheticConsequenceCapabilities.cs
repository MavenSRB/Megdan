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

        /// <summary>
        /// Milestone 8.57c. What rulebook p.13's ASSIST coins are multiplied by for this one Defeat -
        /// Rowenna's Glorious Triumph, "Defeat an enemy hero adjacent to you with no cards in hand;
        /// <b>your friendly heroes gain triple assist coins</b>".
        ///
        /// <para><b>It is a parameter on the request rather than a rule, and RO-Q3 is why.</b> Bain's
        /// <c>AdditionalDefeatLifeCostWhenDefeated</c> is the near miss worth naming: that is a
        /// standing property of a marker its holder wears, derived at the command boundary and true
        /// of every Defeat while it stands. This clause is true of exactly ONE Defeat - the one the
        /// same sentence just asked for - and a modification set has no way to say "this one". So the
        /// multiplier rides with the request, the way the request itself does.</para>
        ///
        /// <para><b>The multiplier is expressed at the spend site rather than paid afterwards.</b>
        /// p.13's assist coins are produced in exactly one place -
        /// <c>HeroDefeatRewardSchedule.AssistCoins</c>, read by <c>ResolveHeroDefeat</c> - and
        /// tripling the number there gives each team-mate ONE grant of the tripled amount. Paying
        /// twice as a second step would show the scoreboard two collections per hero for one Defeat,
        /// and would leave a second mutation for a restore mid-consequence to disagree about. The
        /// same rule Ursafar's Tear is composed under, applied to the other column of the same
        /// table.</para>
        ///
        /// <para><b>ASSIST coins only.</b> The defeating hero collects Defeat coins equal to the
        /// defeated hero's Level, and those are a different sentence on the same page; Life counters
        /// are a third. Neither moves, which is what makes this parameter narrow enough to name.
        /// </para>
        ///
        /// <para>Optional. An unbound parameter is p.13's own schedule - a sentence that multiplies
        /// nothing multiplies by one - so every body composed before this milestone asks for exactly
        /// the Defeat it asked for.</para>
        /// </summary>
        public const string AssistCoinMultiplierParameter = "assistCoinMultiplier";

        /// <summary>
        /// Milestone 8.57c. The multiplier this request carries, published beside
        /// <see cref="DefeatedHeroOutput"/> and only for a node that declared it.
        ///
        /// <para>It is published rather than read back off the definition because a completed node
        /// outcome is this build's transport for a Defeat request: milestone 8.10's remarks on
        /// <c>RequestedHeroDefeatQuery</c> state the reason, and it holds for the number as much as
        /// for the hero - a fact in the completed frame survives a pause, a snapshot restore, a
        /// replay and a retained-prefix Undo without a field of its own. Reading the input from the
        /// trusted definition instead would work for a literal and break the moment a face binds the
        /// number from another node.</para>
        /// </summary>
        public const string AssistCoinMultiplierOutput = "assistCoinMultiplier";

        /// <summary>
        /// Milestone 8.57c. What p.13's assist coins are multiplied by when no card text says
        /// otherwise, which is the whole of this build before Glorious Triumph: one.
        ///
        /// <para>It is named rather than written as a literal at the three places that need it - this
        /// handler, the request the query rebuilds, and the p.14 step-6 entrance that never carries a
        /// clause - because "unmultiplied" is a statement about rulebook p.13 and a bare <c>1</c>
        /// appearing in a spend site is not.</para>
        /// </summary>
        public const int UnmultipliedAssistCoins = 1;

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
                new[]
                {
                    new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero),

                    // Milestone 8.57c. Glorious Triumph's "triple assist coins" - see
                    // AssistCoinMultiplierParameter. Optional, so every body composed before this
                    // milestone asks for exactly the Defeat it asked for.
                    new EffectCapabilityParameter(
                        AssistCoinMultiplierParameter, EffectValueKind.Integer, isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding(DefeatedHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(AssistCoinMultiplierOutput, EffectValueKind.Integer)
                });

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

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(DefeatedHeroOutput, EffectValue.Hero(hero))
                };

                // Only for a node that declared it. An unbound parameter is p.13's own schedule, so
                // the number a declaring node publishes is never less than one - a face that
                // multiplied assist coins by nothing would be a sentence no card prints, and the
                // spend site refuses to be told to pay less than the page.
                if (context.Publishes(AssistCoinMultiplierOutput))
                {
                    facts.Add(new EffectOutcomeFact(
                        AssistCoinMultiplierOutput,
                        EffectValue.Integer(DeclaredMultiplier(context))));
                }

                return EffectExecutionResult.Applied(facts);
            }

            /// <summary>
            /// The multiplier this request carries, as a number the spend site can use: the declared
            /// one where a face prints a clause, and p.13's own schedule - one - where none does.
            /// </summary>
            private static int DeclaredMultiplier(EffectCapabilityContext context)
            {
                var declared = context.Input(AssistCoinMultiplierParameter).IntegerValue;
                return declared < UnmultipliedAssistCoins ? UnmultipliedAssistCoins : declared;
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
                var value = context.Input(MinionParameter);
                if (value.Kind == EffectValueKind.Hero &&
                    context.Modifications?.ModeRules?.HasMinionCategory(value.HeroId) == true)
                    return EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(DefeatedMinionOutput, value)
                    });
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
                (context.Input(MinionParameter).MinionId.IsDefined ||
                 (context.Input(MinionParameter).Kind == EffectValueKind.Hero &&
                  context.Modifications?.ModeRules?.HasMinionCategory(context.Input(MinionParameter).HeroId) == true))
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
