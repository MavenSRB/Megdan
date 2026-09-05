using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z14-blockerid: the engine seam itself. When a capability's <c>Execute()</c> (or a
    /// selection's own narrowing) reports <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>
    /// and the refusing party can explain itself, <see cref="EffectResolutionSession"/> now asks the
    /// SAME question milestone 8.35l's offer-time probe already asks -
    /// <see cref="EffectOptionFeasibilityQuery.TryExplainNodeRefusal"/> - and attaches the answer to
    /// the recorded <see cref="EffectResolutionEvent.Blocking"/>.
    ///
    /// <para>A tiny capability that refuses and explains itself is enough to prove the wiring: which
    /// standing modification a REAL restriction produces is already proven end to end by
    /// <c>SwapAndPlaceRestrictionFeasibilityTests.AssertNamesTheDagger</c> against the real
    /// <c>SwapWithActorHandler</c>/Wasp's Magnetic Dagger, and by
    /// <c>MatchLogChannelTests</c>/<c>PrivateOnlineEventChannelTests</c> for how that answer survives
    /// the rule-event log, the match log's own derivation, and the wire. What was never previously
    /// exercised is whether <see cref="EffectResolutionSession"/> itself asks and carries it at the
    /// exact moment a resolution already under way stops - which is what this file is for.</para>
    /// </summary>
    public sealed class EffectResolutionSessionBlockerIdentityTests
    {
        private static readonly HeroId Actor = new HeroId("hero-sabina");
        private static readonly HeroId Controller = new HeroId("hero-sabina");
        private static readonly HeroId BlockingHero = new HeroId("hero-wasp");
        private static readonly CardInstanceId BlockingCard = new CardInstanceId("card-magnetic-dagger");

        [Test]
        public void ACapabilityThatRefusesAndCanExplainItself_HasItsExplanationCarriedOnTheEvent()
        {
            var handler = new ExplainingRefusalHandler();
            var definition = Definition(Leaf("root", handler));

            var result = Start(definition, handler);

            Assert.That(result.IsComplete, Is.True,
                "A mandatory node reporting ImpossibleMandatory stops the whole resolution.");
            var stopped = result.Events.Single(value => value.NodeId.Equals(new EffectNodeId("root")));
            Assert.That(stopped.ResultKind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(stopped.Blocking, Is.Not.Null,
                "The handler could explain itself, so the transient event must carry what it said.");
            Assert.That(stopped.Blocking.ControllerHeroId, Is.EqualTo(BlockingHero));
            Assert.That(stopped.Blocking.SourceCardInstanceId, Is.EqualTo(BlockingCard));
        }

        [Test]
        public void ACapabilityThatRefusesWithNothingToExplain_CarriesNoBlockingModification()
        {
            var handler = new SilentlyRefusingHandler();
            var definition = Definition(Leaf("root", handler));

            var result = Start(definition, handler);

            var stopped = result.Events.Single(value => value.NodeId.Equals(new EffectNodeId("root")));
            Assert.That(stopped.ResultKind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(stopped.Blocking, Is.Null,
                "Nothing here may invent a blocker the handler never named - the same honest " +
                "absence milestone 8.35l's own ActionUnavailability.None already leaves.");
        }

        /// <summary>
        /// A capability that APPLIES carries no blocking modification either - the field means
        /// something only beside a stop, exactly as <see cref="EffectExecutionResult.Outcomes"/>
        /// means something only beside a result that published facts.
        /// </summary>
        [Test]
        public void ACapabilityThatApplies_CarriesNoBlockingModification()
        {
            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticEffectCapabilities.NoteActorV1);
            var definition = Definition(Leaf("root", handler));

            var result = Start(definition, handler);

            Assert.That(result.IsComplete, Is.True);
            var applied = result.Events.Single(value => value.NodeId.Equals(new EffectNodeId("root")));
            Assert.That(applied.ResultKind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(applied.Blocking, Is.Null);
        }

        // -----------------------------------------------------------------------------------
        // Fixture handlers
        // -----------------------------------------------------------------------------------

        /// <summary>A capability that always refuses, and can always explain why.</summary>
        private sealed class ExplainingRefusalHandler
            : IEffectCapabilityHandler, IEffectRefusalExplainingCapabilityHandler
        {
            public EffectCapabilityReference Reference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("test-explaining-refusal"), 1);

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;

            public bool TryExplainRefusal(
                EffectCapabilityContext context, out AppliedRuleModification blocking)
            {
                blocking = AppliedRuleModification.FromActiveEffect(
                    RuleModification.ForRestriction(
                        RuleActivity.Swap,
                        RestrictedActorScope.Of(RestrictedActorRelation.AffectedObjectItself)),
                    RuleObject.ForHero(Actor),
                    new ActiveEffectId("magnetic-dagger-effect-1"),
                    BlockingHero,
                    TeamSide.Red,
                    orderingSequence: 1,
                    sourceCardInstanceId: BlockingCard);
                return true;
            }
        }

        /// <summary>A capability that always refuses and never explains why - the honest absence.</summary>
        private sealed class SilentlyRefusingHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("test-silent-refusal"), 1);

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;
        }

        // -----------------------------------------------------------------------------------
        // The same scaffolding EffectResolutionSessionTests uses, restated for a one-node
        // definition bound to a caller-supplied handler rather than a fixed synthetic one.
        // -----------------------------------------------------------------------------------

        private static EffectResolutionStepResult Start(
            EffectDefinition definition, IEffectCapabilityHandler handler) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("session-fixture"),
                new CardInstanceId("card-back-to-back"),
                default,
                new ActionDefinitionId("session-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                Context(definition, handler));

        private static EffectResolutionContext Context(
            EffectDefinition definition, IEffectCapabilityHandler handler) =>
            new EffectResolutionContext(
                definition,
                new EffectCapabilityCatalog(new[] { handler }),
                null, null, 1, 1, RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static EffectDefinition Definition(EffectNodeDefinition root) =>
            new EffectDefinition(
                new EffectDefinitionId("session-fixture-definition"), "rev-1", root.Id,
                new[] { root });

        private static EffectNodeDefinition Leaf(string id, IEffectCapabilityHandler handler) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                handler.Reference);
    }
}
