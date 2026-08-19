using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8d: every Active-effect creator mints a per-activation instance identity through
    /// <see cref="ActiveEffectInstanceIdentity"/>.
    ///
    /// The proofs run against every creator rather than one representative: the five modification
    /// capabilities - including <c>GrantPushImmunityV1</c> and <c>CountAsMinionV1</c>, which no
    /// configured card binds yet - and 6.2a's MarkActor. Each is executed as the production handler
    /// object the catalog registers, against a live lifecycle, so what is proved is the capability's
    /// own creation path rather than a re-implementation of it.
    /// </summary>
    public sealed class ActiveEffectInstanceIdentityTests
    {
        private static readonly HeroId Actor =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        [Test]
        public void EveryCreator_MintsAFreshInstanceForALaterActivationDespiteASpentOne()
        {
            foreach (var handler in AllCreators())
            {
                var definition = handler.ActiveEffectDefinitions.Single();
                var timing = new ActiveEffectLifecycle();
                var participants = PlaceholderOneVsOneParticipants.Create();

                handler.Execute(Context(timing, participants, 1, 1));
                Assert.That(timing.Effects, Has.Count.EqualTo(1), definition.DefinitionId);

                timing.ApplyCheckpoint(EndContextFor(definition), new TieBreakerState(TeamSide.Red));
                Assert.That(timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended),
                    definition.DefinitionId + ": the first activation must be spent before the " +
                    "replay, or this proves nothing about spent-instance collisions.");

                handler.Execute(Context(timing, participants, 2, 1));

                Assert.That(timing.Effects, Has.Count.EqualTo(2),
                    definition.DefinitionId + ": a later activation is a new instance; the spent " +
                    "instance was a different activation and must not suppress it.");
                Assert.That(timing.Effects.Select(effect => effect.Id).Distinct().Count(),
                    Is.EqualTo(2), definition.DefinitionId);
                Assert.That(
                    timing.Effects.Single(effect => effect.CreatedRoundIndex == 2).Phase,
                    Is.Not.EqualTo(ActiveEffectPhase.Ended), definition.DefinitionId);
            }
        }

        [Test]
        public void ReExecutingTheSameActivationInsideItsOwnResolution_StaysALegalNoOp()
        {
            foreach (var handler in AllCreators())
            {
                var definition = handler.ActiveEffectDefinitions.Single();
                var timing = new ActiveEffectLifecycle();
                var participants = PlaceholderOneVsOneParticipants.Create();

                handler.Execute(Context(timing, participants, 1, 1));
                handler.Execute(Context(timing, participants, 1, 1));

                Assert.That(timing.Effects, Has.Count.EqualTo(1),
                    definition.DefinitionId + ": the same activation re-executed inside its own " +
                    "resolution composes the same identity and creates nothing twice.");
            }
        }

        [Test]
        public void TheContract_ComposesTheSameIdForTheSameActivation()
        {
            var definition = FirstDefinition();
            var timing = new ActiveEffectLifecycle();
            var participants = PlaceholderOneVsOneParticipants.Create();

            Assert.That(
                ActiveEffectInstanceIdentity.Mint(Context(timing, participants, 2, 3), definition),
                Is.EqualTo(ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 2, 3), definition)),
                "Identical activation anchors must compose an identical ID, or within-resolution " +
                "re-execution stops being a no-op.");
        }

        [Test]
        public void TheContract_DistinguishesEveryActivationAnchor()
        {
            var definition = FirstDefinition();
            var other = LastDefinition();
            var timing = new ActiveEffectLifecycle();
            var participants = PlaceholderOneVsOneParticipants.Create();

            var baseline = ActiveEffectInstanceIdentity.Mint(
                Context(timing, participants, 2, 3), definition);
            var variants = new[]
            {
                ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 3, 3), definition),
                ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 2, 4), definition),
                ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 2, 3, cardId: "identity-other-card"), definition),
                ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 2, 3, actionId: "identity-other-action"),
                    definition),
                ActiveEffectInstanceIdentity.Mint(
                    Context(timing, participants, 2, 3, nodeId: "identity-other-node"), definition),
                ActiveEffectInstanceIdentity.Mint(Context(timing, participants, 2, 3), other)
            };

            Assert.That(variants, Is.Unique,
                "Each varied anchor must produce its own identity.");
            Assert.That(variants, Has.None.EqualTo(baseline),
                "Round, turn, card, action, node, and definition each distinguish the identity; an " +
                "anchor the composition ignores is an activation it cannot distinguish.");
        }

        private static ActiveEffectDefinition FirstDefinition() =>
            SyntheticModificationCapabilities.Definitions[0];

        private static ActiveEffectDefinition LastDefinition() =>
            SyntheticModificationCapabilities.Definitions[
                SyntheticModificationCapabilities.Definitions.Count - 1];

        private static IEffectCapabilityHandler[] AllCreators() =>
            SyntheticModificationCapabilities.Handlers()
                .Concat(new[]
                {
                    SyntheticEffectCapabilities.CreateCatalog()
                        .Load(SyntheticEffectCapabilities.MarkActorV1)
                })
                .ToArray();

        /// <summary>
        /// The five modification definitions cancel on end-of-round retrieval; the Mark definition
        /// cancels when its own source card changes state. Either cancellation leaves a spent
        /// instance whose ID stays reserved, which is the collision the milestone removes.
        /// </summary>
        private static ActiveEffectCheckpointContext EndContextFor(
            ActiveEffectDefinition definition) =>
            definition.CancellationConditions.Contains(
                ActiveEffectCancellationCondition.CardsRetrieved)
                ? new ActiveEffectCheckpointContext(TimingCheckpoint.CardsRetrieved, 1, 4)
                : new ActiveEffectCheckpointContext(
                    TimingCheckpoint.SourceCardStateChanged, 1, 4,
                    sourceCardInstanceId: new CardInstanceId("identity-card"));

        private static EffectCapabilityContext Context(
            ActiveEffectLifecycle timing,
            MatchParticipants participants,
            int roundIndex,
            int turnIndex,
            string cardId = "identity-card",
            string actionId = "identity-action",
            string nodeId = "identity-node") =>
            new EffectCapabilityContext(
                new EffectNodeId(nodeId),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId(cardId),
                new CardDefinitionReference(new CardDefinitionId("fixture-radius-context"), "fixture-rev-1"),
                new ActionDefinitionId(actionId),
                null,
                roundIndex,
                turnIndex,
                participants,
                timing,
                RuleModificationSet.Empty, EffectCardState.Absent);
    }
}
