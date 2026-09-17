using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38c2. The distinction Swift's Bullet Time turns on - "After you resolve a basic
    /// CARD" - proved before her content exists.
    ///
    /// <para><b>It is not the distinction <c>WasRepeatedInstance</c> draws, and that is the whole
    /// point.</b> Tigerclaw's Cloak and Daggers reads "After you perform a basic ACTION you may repeat
    /// it once" and asks what KIND of thing just finished. Bullet Time asks whether the CARD was
    /// resolved - once a turn, however many actions the turn performs - and then grants that card's
    /// printed primary, which need not be the action that was performed. Widening the existing
    /// condition would have fired her Ultimate after a repeat, after a borrowed primary, and after
    /// every action a grant produced.</para>
    /// </summary>
    [TestFixture]
    public sealed class BasicCardResolutionTriggerTests
    {
        /// <summary>
        /// Every invocation context is classified, and the case enumerates the enum rather than listing
        /// the ones it happens to know - so a context added later fails here instead of falling
        /// silently into "not a resolution".
        /// </summary>
        [Test]
        public void EveryInvocationContext_IsClassifiedAsAResolutionOrNot()
        {
            var contexts = Enum.GetValues(typeof(ActionInvocationContext))
                .Cast<ActionInvocationContext>()
                .ToArray();

            Assert.That(contexts.Length, Is.EqualTo(8),
                "If a context has been added, classify it in BasicActionQuery.IsCardResolution.");

            var resolutions = contexts
                .Where(BasicActionQuery.IsCardResolution)
                .ToArray();

            Assert.That(
                resolutions,
                Is.EquivalentTo(new[]
                {
                    ActionInvocationContext.PrintedPrimary,
                    ActionInvocationContext.PrintedSecondary,
                    ActionInvocationContext.GeneralRuleSecondary,
                    ActionInvocationContext.Replacement
                }),
                "p.10's \"resolve your card\" is the printed primary, either secondary, and p.7's " +
                "replacement actions - which are taken INSTEAD of the printed action and resolve the " +
                "card all the same.");
        }

        /// <summary>
        /// The three that are not, named individually because each is a different reason and a future
        /// reader should not have to infer them from the set above.
        /// </summary>
        [Test]
        public void ARepeatAGrantAndAReaction_AreNotCardResolutions()
        {
            Assert.That(
                BasicActionQuery.IsCardResolution(ActionInvocationContext.Repeated), Is.False,
                "A repeat is a second performance off a card already resolved.");
            Assert.That(
                BasicActionQuery.IsCardResolution(ActionInvocationContext.TextGranted), Is.False,
                "So is a borrowed or granted primary - which is what stops Bullet Time's own grant " +
                "from re-triggering it.");
            Assert.That(
                BasicActionQuery.IsCardResolution(ActionInvocationContext.Reaction), Is.False,
                "A p.14 Defense is spent on somebody else's turn and resolves nobody's card.");
        }

        /// <summary>
        /// An undefined context is refused rather than treated as false. A silent false here would be a
        /// card resolution the engine forgot to notice, which is the failure this Ultimate cannot see.
        /// </summary>
        [Test]
        public void AnUndeclaredContext_IsRefusedRatherThanTreatedAsNotAResolution()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BasicActionQuery.IsCardResolution((ActionInvocationContext)99));
        }

        // -------------------------------------------------------------------------------------------
        // The fact the condition reads
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The fact travels on the completion, beside the other three the trigger conditions read, and
        /// defaults to false so every producer written before this milestone keeps its meaning.
        /// </summary>
        [Test]
        public void ThePerformedFact_CarriesWhetherItWasACardResolution()
        {
            var resolved = PerformedBasicAction.Of(
                new GuardsDigital.Core.Units.Heroes.HeroId("swift-trigger-actor"),
                new CardInstanceId("swift-trigger-card"),
                new ActionDefinitionId("swift-trigger-action"),
                ActionKind.Skill,
                wasCardResolution: true);

            var granted = PerformedBasicAction.Of(
                new GuardsDigital.Core.Units.Heroes.HeroId("swift-trigger-actor"),
                new CardInstanceId("swift-trigger-card"),
                new ActionDefinitionId("swift-trigger-action"),
                ActionKind.Skill);

            Assert.That(resolved.WasCardResolution, Is.True);
            Assert.That(granted.WasCardResolution, Is.False,
                "The default is false, so every producer written before this milestone keeps meaning " +
                "exactly what it meant.");
            Assert.That(resolved.WasRepeatedInstance, Is.False,
                "and the new fact is independent of the repeat flag rather than derived from it.");
        }

        /// <summary>
        /// The condition reads the fact and nothing else - not the action's kind, because a basic card
        /// resolved by a Hold is still resolved and the primary this Ultimate grants is the card's own.
        /// </summary>
        [Test]
        public void TheConditionIgnoresTheActionKind()
        {
            foreach (var kind in new[] { ActionKind.Skill, ActionKind.Attack, ActionKind.Movement })
            {
                var performed = PerformedBasicAction.Of(
                    new GuardsDigital.Core.Units.Heroes.HeroId("swift-trigger-actor"),
                    new CardInstanceId("swift-trigger-card"),
                    new ActionDefinitionId("swift-trigger-action"),
                    kind,
                    wasCardResolution: true);

                Assert.That(performed.WasCardResolution, Is.True, kind.ToString());
            }
        }

        /// <summary>
        /// The fifth condition exists and is distinct from the four before it, so a content author
        /// naming it cannot silently get Cloak and Daggers' reading.
        ///
        /// <para>Milestone 8.39f1 extended the count rather than renumbering it: a sixth condition
        /// arrived - Cutter's "after you perform a primary action" - and this case is the tripwire that
        /// is supposed to notice one. <c>PrimaryActionTriggerTests</c> owns what the sixth means.</para>
        ///
        /// <para>Milestone 8.40h extended it again, and the tripwire did its job: a seventh condition
        /// arrived - Bain's "after you give a hero the bounty marker" - and it is the first that is not
        /// matched against a completed action at all, which is why
        /// <c>UltimateTriggeredEffectSourceQuery</c> answers it <c>false</c> and
        /// <c>MarkerGivenTriggerTests</c> owns what it means.</para>
        /// </summary>
        [Test]
        public void TheFifthTriggerCondition_IsDistinctFromTheFourBeforeIt()
        {
            var conditions = Enum.GetValues(typeof(UltimateTriggerCondition))
                .Cast<UltimateTriggerCondition>()
                .Where(condition => condition != UltimateTriggerCondition.None)
                .ToArray();

            Assert.That(conditions.Length, Is.EqualTo(8),
                "Eight since 8.41c adds the distinct resolved-discard operation. A new condition is " +
                "classified in UltimateTriggeredEffectSourceQuery and proved in its own fixture.");
            Assert.That(
                conditions, Does.Contain(UltimateTriggerCondition.AfterControllerResolvesBasicCard));
            Assert.That(
                UltimateTriggerCondition.AfterControllerResolvesBasicCard,
                Is.Not.EqualTo(UltimateTriggerCondition.AfterControllerPerformsBasicAction));
        }
    }
}
