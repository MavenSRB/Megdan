using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's contract for the one question his three split faces ask - "if you TARGET a
    /// hero who is adjacent to 3 or more obstacles" - as a fact about the Attack that is running rather
    /// than about a hero somebody named. Milestone 8.69b.
    ///
    /// <para><b>Why this is a second predicate beside milestone 8.69a's, and not a call to it.</b>
    /// 8.69a's <c>brynn-hero-adjacent-to-obstacles@1</c> answered about a hero a node bound, wherever
    /// that hero was standing, and answered NOTHING about a hero standing nowhere - which its own
    /// remarks stated as the reading it wanted: "a hero who has left the board is not the hero the
    /// clause is about". That is exactly right for a clause whose subject is a hero. It is exactly
    /// wrong for these three,
    /// because their subject is the TARGETING, which already happened. Rulebook p.14 removes a defeated
    /// hero at step 6 and applies "After the attack:" text at step 7, so Peak Precision's retrieval -
    /// gated on the same condition as its +2, BR-Q7 - is asked about a target the successful attack has
    /// just taken off the board. A gate reading the live hero would refuse the retrieval precisely when
    /// the attack worked, which no reading of the printed sentence produces.</para>
    ///
    /// <para><b>So the question is asked of the Attack, and answered about the target as the Attack
    /// bound it</b> - BR-Q5's "when it was chosen", and milestone 8.19a's
    /// <c>TargetSpaceBeforeConsequences</c>, which that milestone captured "precisely so a rider can
    /// name it after a defeat" and which Brogan's Onslaught, Sabina's Point Blank Shot and Min's
    /// Stances already read at this step. A target still standing is read where it stands, which is the
    /// same space; only a target the page has removed falls back to the captured one. Nothing is
    /// recorded for this, no field is added and no snapshot moves.</para>
    ///
    /// <para><b>The threshold itself is not re-derived.</b> Every branch below goes to
    /// <see cref="ObstacleAdjacencyQuery"/>, milestone 8.69a's single owner, so p.19's water ring,
    /// milestone 8.7's overlay and Over the Top's counts-as reach this clause without this file knowing
    /// any of them exist.</para>
    ///
    /// <para>Milestone 8.69f took that reading to its conclusion and DROPPED 8.69a's predicate, which
    /// no printed sentence of his reaches - a capability nothing can start is one a set drops rather
    /// than one a dealing registers. This one stands.</para>
    ///
    /// <para><b>Nothing here is registered</b>, on milestone 7.9's gate and for the reason the 8.69a
    /// partial states at length: <see cref="SplitAttackRegisteredHandlers"/> is empty,
    /// <see cref="SplitAttackDeferredHandlers"/> is the whole list until 8.69g deals him, and
    /// <c>BrynnSplitAttackTests</c> asserts the gate from both sides. 8.69f aggregates this block's two
    /// lists into the hero-wide ones in <c>BrynnComponentContent.cs</c>.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// "If you target a hero who is adjacent to 3 or more obstacles" - Split Attack's, Split
        /// Throw's and Peak Precision's shared condition, asked of the enclosing Attack.
        ///
        /// <para>A presence check and never a target, on p.19's own sentence: asking changes nothing
        /// and p.18's immunity never enters it.</para>
        /// </summary>
        public static EffectCapabilityId AttackTargetWasAdjacentToObstacles { get; } =
            new EffectCapabilityId("brynn-attack-target-was-adjacent-to-obstacles");

        public static EffectCapabilityReference AttackTargetWasAdjacentToObstaclesV1 =>
            new EffectCapabilityReference(AttackTargetWasAdjacentToObstacles, 1);

        /// <summary>
        /// Nothing this build registers, until 8.69g deals him. The member exists so a "registered or
        /// deferred" sweep keeps its shape and so the fixture asserting the deferral asks a question
        /// rather than failing to compile.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SplitAttackRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The one predicate this block declares and this build withholds. It is a new handler TYPE, so
        /// 8.69g owes it a showcase consumer as well as a registration.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SplitAttackDeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new AttackTargetWasAdjacentToObstaclesHandler()
            };

        /// <summary>
        /// "a hero who is adjacent to 3 or more obstacles", asked about the unit the enclosing Attack
        /// took, as it stood when the Attack took it - whole.
        ///
        /// <para><b>A minion target answers FALSE rather than nothing.</b> p.13 lets an Attack take
        /// either kind of unit and all three faces print "a unit" in their first sentence, so an Attack
        /// that took a minion is an ordinary Attack whose condition simply does not hold - the reading
        /// <c>synthetic-attack-took-a-hero@1</c> already takes of the same fact. What IS refused is a
        /// composition with no Attack around it or no board to ask, because "this cannot be asked" and
        /// "the answer is no" are different facts and only one of them is a rule.</para>
        /// </summary>
        private sealed class AttackTargetWasAdjacentToObstaclesHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => AttackTargetWasAdjacentToObstaclesV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[] { new EffectOutputBinding(PresentOutputName, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var attack = context.EnclosingAttack;
                var spaces = context.Components?.Tokens;
                if (!attack.IsDefined || spaces == null)
                {
                    return null;
                }

                var target = attack.HeroTargetId;
                if (!target.IsDefined)
                {
                    // A minion target, or an Attack that has not bound one yet: "a hero" is not
                    // satisfied, and that is an answer.
                    return false;
                }

                // Where the hero is now, which for every target the page did not remove is the space
                // the Attack bound - an Attack does not move its defender. The overlay is inside this
                // call, so Over the Top reaches the clause without this handler naming an Ultimate.
                if (ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                        spaces, target, context.Modifications))
                {
                    return true;
                }

                // p.14 step 6 has removed the target, and the clause is about the moment it was
                // chosen. Milestone 8.19a captured that space for exactly this.
                return !spaces.TryGetHeroCoordinate(target, out _) &&
                    attack.TargetSpaceBeforeConsequences.HasValue &&
                    ObstacleAdjacencyQuery.IsAdjacentToThreeOrMoreObstacles(
                        spaces,
                        attack.TargetSpaceBeforeConsequences.Value,
                        context.Modifications);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// Asking what the Attack took changes nothing - milestone 8.35l's defect, which both of
            /// 8.69a's terrain predicates record for the same reason.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var value = Read(context);
                return value.HasValue
                    ? new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
                    }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }
    }
}
