using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a acceptance check 5: snapshot payloads supply runtime state, not definition
    /// semantics, and a payload whose trusted revisions are missing or incompatible is rejected
    /// atomically.
    ///
    /// These tamper with the trusted side rather than the bytes, which is the pattern the card- and
    /// map-revision fixtures already use and the more realistic threat: the same payload arriving at a
    /// build whose registered content differs. Byte-level tampering is covered separately by the
    /// undefined-enum and schema-version cases in the serializer fixture.
    /// </summary>
    public sealed class EffectSnapshotTamperingTests
    {
        [Test]
        public void Restore_BindsActiveEffectSemanticsFromTheCatalogAndNotThePayload()
        {
            var restored = Deserialize(CapturePayloadWithLiveActiveEffect(), Catalog());

            var effect = restored.Round.Timing.Effects.Single();
            Assert.That(effect.Definition, Is.SameAs(SyntheticEffectCapabilities.MarkDefinition),
                "The restored effect must be bound to the catalog's own definition object. Reference " +
                "equality is the strongest simple statement that nothing was rebuilt from the payload.");
            Assert.That(effect.ControllerHeroId.IsDefined, Is.True,
                "Runtime bindings still come from the payload; only semantics come from the catalog.");
        }

        [Test]
        public void Restore_RejectsAnUnknownCapabilityId()
        {
            var payload = CapturePayloadWithLiveActiveEffect();

            var rejection = Assert.Throws<SnapshotLoadException>(() =>
                Deserialize(payload, CatalogWithout(SyntheticEffectCapabilities.MarkActorV1)));

            Assert.That(rejection.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.EffectCapabilityMissing));
        }

        [Test]
        public void Restore_RejectsAnUnregisteredCapabilityRevision()
        {
            var payload = CapturePayloadWithLiveActiveEffect();

            var rejection = Assert.Throws<SnapshotLoadException>(() =>
                Deserialize(payload, CatalogWithMarkAtRevision(2)));

            Assert.That(rejection.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.EffectCapabilityRevisionMismatch),
                "A registered capability at a different revision is a version problem, not a missing " +
                "one, and a restore must be able to say which.");
        }

        [Test]
        public void Restore_RejectsAnActiveEffectWhoseDefinitionIsNotInTheTrustedCatalog()
        {
            var payload = CapturePayloadWithLiveActiveEffect();

            var rejection = Assert.Throws<SnapshotLoadException>(() =>
                Deserialize(payload, CatalogWithMarkDeclaringNoEffect()));

            Assert.That(rejection.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.EffectDefinitionMissing),
                "The capability set matches, so registration validation passes; the effect's semantics " +
                "are still absent, and the payload cannot supply them.");
        }

        [Test]
        public void EveryRejectedRestore_LeavesTheLiveSessionExactlyAsItWas()
        {
            var session = CreateSkillSession();
            RunSkillUntilPaused(session);
            var before = session.Projection;
            var beforeSnapshot = session.CaptureSnapshot();

            // The session restores through its own catalog, so a tampered payload is the way to make it
            // refuse; a truncated one is the simplest tamper the envelope cannot explain away.
            var refused = session.RestoreSnapshot(beforeSnapshot.Substring(0, beforeSnapshot.Length - 8));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeSnapshot),
                "A refused restore must leave the authoritative payload byte-identical.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The paused effect frame must survive a refused restore intact.");
        }

        private static RestoredAuthoritativeMatch Deserialize(
            string authoritativePayload,
            EffectCapabilityCatalog capabilities)
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            return AuthoritativeSnapshotSerializer.Deserialize(
                new MatchContentCatalogs(capabilities, configuration.ContentCatalogs.Components),
                authoritativePayload,
                configuration.CardDefinitions,
                new[] { map },
                setup.Minions);
        }

        /// <summary>Runs the synthetic Skill far enough that its Active effect exists and is serialized.</summary>
        private static string CapturePayloadWithLiveActiveEffect()
        {
            var session = CreateSkillSession();
            RunSkillUntilPaused(session);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.Not.Null);
            return session.Projection.AuthoritativeSnapshot;
        }

        private static void RunSkillUntilPaused(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("focus"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var skill = session.Projection.AvailableTurnActions.Single(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Skill);
            Accept(session, DirectorCommand.StartAction(skill.ActionDefinitionId));
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null);
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static EffectCapabilityCatalog Catalog() =>
            MatchContentCatalogs.ForThisBuild().Capabilities;

        private static EffectCapabilityCatalog CatalogWithout(EffectCapabilityReference omitted) =>
            new EffectCapabilityCatalog(Handlers()
                .Where(handler => !handler.Reference.Equals(omitted)));

        private static EffectCapabilityCatalog CatalogWithMarkAtRevision(int revision) =>
            new EffectCapabilityCatalog(Handlers()
                .Where(handler => !handler.Reference.Equals(SyntheticEffectCapabilities.MarkActorV1))
                .Concat(new IEffectCapabilityHandler[]
                {
                    new SubstituteMarkHandler(
                        new EffectCapabilityReference(SyntheticEffectCapabilities.MarkActor, revision),
                        new[] { SyntheticEffectCapabilities.MarkDefinition })
                }));

        private static EffectCapabilityCatalog CatalogWithMarkDeclaringNoEffect() =>
            new EffectCapabilityCatalog(Handlers()
                .Where(handler => !handler.Reference.Equals(SyntheticEffectCapabilities.MarkActorV1))
                .Concat(new IEffectCapabilityHandler[]
                {
                    new SubstituteMarkHandler(
                        SyntheticEffectCapabilities.MarkActorV1,
                        Array.Empty<ActiveEffectDefinition>())
                }));

        /// <summary>Every registered handler, reached through the catalog's own ordered reference list.</summary>
        private static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            return catalog.Registered.Select(reference => catalog.Load(reference)).ToArray();
        }

        private sealed class SubstituteMarkHandler : IEffectCapabilityHandler
        {
            private readonly IReadOnlyList<ActiveEffectDefinition> effects;

            public SubstituteMarkHandler(
                EffectCapabilityReference reference,
                IReadOnlyList<ActiveEffectDefinition> effects)
            {
                Reference = reference;
                this.effects = effects;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions => effects;

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("marked", EffectValue.Hero(context.ActorId)) });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        private static MatchSession CreateSkillSession()
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
