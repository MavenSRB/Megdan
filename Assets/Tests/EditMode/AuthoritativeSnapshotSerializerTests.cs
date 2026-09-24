using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AuthoritativeSnapshotSerializerTests
    {
        [Test]
        public void RoundTrip_DynamicallyRepositionedObstacleRetainsIdentityAndCanMoveAgain()
        {
            var fixture = CreateFixture();
            var obstacle = new GuardsDigital.Core.Obstacles.ObstacleId("snapshot-rock");
            var moved = new HexCoordinate(0, 1);
            Assert.That(fixture.Board.TryPlaceOrRepositionObstacle(obstacle, moved), Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(), payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });

            Assert.That(restored.Board.ObstacleOccupancies.Single().ObstacleId,
                Is.EqualTo(obstacle));
            Assert.That(restored.Board.ObstacleOccupancies.Single().Coordinate,
                Is.EqualTo(moved));
            Assert.That(restored.Board.TryPlaceOrRepositionObstacle(
                obstacle, new HexCoordinate(-1, 1)), Is.True);
            Assert.That(restored.Board.ObstacleOccupancies, Has.Count.EqualTo(1));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board),
                Is.EqualTo(payload), "The restored board mutation cannot change the source snapshot.");
        }

        [Test]
        public void RoundTrip_PreservesParticipantsBoardCardsCommitmentAndLogicalContinuation()
        {
            var fixture = CreateFixture();
            var red = fixture.Configuration.RedHand.Instances[0];
            var blue = fixture.Configuration.BlueHand.Instances[0];
            Assert.That(fixture.Round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });

            Assert.That(restored.Participants.Teams.Select(team => (team.Id.Value, team.Side)),
                Is.EqualTo(fixture.Configuration.Participants.Teams.Select(team => (team.Id.Value, team.Side))));
            Assert.That(restored.Participants.Players.Select(player => (player.Id.Value, player.TeamId.Value)),
                Is.EqualTo(fixture.Configuration.Participants.Players.Select(player => (player.Id.Value, player.TeamId.Value))));
            Assert.That(restored.Participants.Heroes.Select(hero => (hero.Id.Value, hero.PlayerId.Value)),
                Is.EqualTo(fixture.Configuration.Participants.Heroes.Select(hero => (hero.Id.Value, hero.PlayerId.Value))));
            Assert.That(restored.Board.BoardCoordinates, Is.EquivalentTo(fixture.Board.BoardCoordinates));
            Assert.That(restored.Board.Occupancies, Is.EquivalentTo(fixture.Board.Occupancies));
            Assert.That(restored.Board.ObstacleOccupancies, Is.EquivalentTo(fixture.Board.ObstacleOccupancies));
            Assert.That(restored.Cards.Select(CardFact), Is.EqualTo(fixture.Configuration.CardInstances.Select(CardFact)));
            Assert.That(restored.Round.RoundIndex, Is.EqualTo(1));
            Assert.That(restored.Round.TurnIndex, Is.EqualTo(1));
            Assert.That(restored.Round.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(restored.Round.CurrentCommitmentAuthority.GetStatus(red.OwnerId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload), "An unchanged restored state must serialize deterministically.");

            var originalEvents = fixture.Round.TryCommitCard(blue.OwnerId, blue.Id).Events;
            var restoredBlue = restored.Cards.Single(card => card.Id.Equals(blue.Id));
            var restoredEvents = restored.Round.TryCommitCard(restoredBlue.OwnerId, restoredBlue.Id).Events;
            Assert.That(EventFacts(restoredEvents), Is.EqualTo(EventFacts(originalEvents)));
            Assert.That(restored.Round.Phase, Is.EqualTo(fixture.Round.Phase));
            Assert.That(restored.Round.TieBreakerState.RevealedSide,
                Is.EqualTo(fixture.Round.TieBreakerState.RevealedSide));
        }

        [Test]
        public void RoundTrip_PendingAttackTargetAndDefenseResumeWithSameOrderedEvents()
        {
            var fixture = CreateFixture();
            CommitFirstCards(fixture);
            var selected = fixture.Round.CurrentTurnAuthority.SelectedActor;
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    selected.ActorId,
                    selected.CardInstanceId)
                .Single(candidate => candidate.AttackRange != AttackRangeKind.None);
            Assert.That(fixture.Round.TryStartAttack(new ActionResolutionId("snapshot-attack"), offer).Accepted, Is.True);

            var targetSnapshot = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var targetRestore = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                targetSnapshot,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            var target = fixture.Round.CurrentTurnAuthority.PendingAttackTargetChoice.LegalTargetHeroIds.Single();
            var originalTargetEvents = fixture.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                new ActionResolutionId("snapshot-attack"),
                fixture.Round.CurrentTurnAuthority.PendingAttackTargetChoice.ChoiceId,
                target)).Events;
            var restoredTargetChoice = targetRestore.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            var restoredTargetEvents = targetRestore.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                restoredTargetChoice.ResolutionId,
                restoredTargetChoice.ChoiceId,
                target)).Events;
            Assert.That(EventFacts(restoredTargetEvents), Is.EqualTo(EventFacts(originalTargetEvents)));

            var defenseSnapshot = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var defenseRestore = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                defenseSnapshot,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            var originalDefense = fixture.Round.CurrentTurnAuthority.PendingDefenseChoice;
            var restoredDefense = defenseRestore.Round.CurrentTurnAuthority.PendingDefenseChoice;
            var originalDefenseEvents = fixture.Round.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(originalDefense.ResolutionId, originalDefense.ChoiceId)).Events;
            var restoredDefenseEvents = defenseRestore.Round.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(restoredDefense.ResolutionId, restoredDefense.ChoiceId)).Events;
            Assert.That(EventFacts(restoredDefenseEvents), Is.EqualTo(EventFacts(originalDefenseEvents)));
            Assert.That(defenseRestore.Round.Phase, Is.EqualTo(fixture.Round.Phase));
            Assert.That(defenseRestore.Cards.Select(CardFact),
                Is.EqualTo(fixture.Configuration.CardInstances.Select(CardFact)));
        }

        [Test]
        public void RoundTrip_PreservesCompletedRoundHistorySlotsAndEndOfRoundCursorOutcomes()
        {
            var fixture = CreateFixture();
            CompleteRoundWithHolds(fixture.Round, fixture.Configuration);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            var cursorRestore = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board),
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            Assert.That(cursorRestore.Round.CurrentEndOfRoundStage,
                Is.EqualTo(fixture.Round.CurrentEndOfRoundStage));
            Assert.That(cursorRestore.Round.EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind)),
                Is.EqualTo(fixture.Round.EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind))));
            Assert.That(SlotFacts(cursorRestore.Round.TurnSlots), Is.EqualTo(SlotFacts(fixture.Round.TurnSlots)));

            while (fixture.Round.Phase == RoundResolutionPhase.EndOfRound)
                Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            var completedRestore = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board),
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            Assert.That(completedRestore.Round.CompletedRounds.Count, Is.EqualTo(1));
            Assert.That(SlotFacts(completedRestore.Round.CompletedRounds[0].TurnSlots),
                Is.EqualTo(SlotFacts(fixture.Round.CompletedRounds[0].TurnSlots)));
            Assert.That(completedRestore.Round.CompletedRounds[0].EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind)),
                Is.EqualTo(fixture.Round.CompletedRounds[0].EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind))));
        }

        [Test]
        public void Deserialize_RejectsSchemaCardAndMapContentMismatchesIndependently()
        {
            var fixture = CreateFixture();
            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var wrongSchema = RewriteSchemaVersion(payload, AuthoritativeSnapshotSerializer.CurrentSchemaVersion + 1);
            var schemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), wrongSchema, fixture.Configuration.CardDefinitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(schemaException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion));

            var definitions = fixture.Configuration.CardDefinitions.ToArray();
            var original = definitions[0];
            definitions[0] = new CardDefinition(
                original.Id,
                original.ContentRevision + "-changed",
                original.DisplayName,
                original.Initiative,
                original.ContentAuthority,
                original.Progression,
                original.ActionDefinitions);
            var revisionException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), payload, definitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(revisionException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch));

            var missingMapException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), payload, fixture.Configuration.CardDefinitions,
                    Array.Empty<BoardMapDefinition>()));
            Assert.That(missingMapException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MapDefinitionMissing));

            var currentMap = fixture.Board.MapDefinition;
            var changedMap = new BoardMapDefinition(currentMap.Id, currentMap.ContentRevision + 1,
                currentMap.DisplayName, currentMap.Spaces, currentMap.Zones, currentMap.SpawnPoints,
                currentMap.OrderedLaneZoneIds);
            var mapRevisionException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), payload, fixture.Configuration.CardDefinitions,
                    new[] { changedMap }));
            Assert.That(mapRevisionException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.MapDefinitionContentRevisionMismatch));
        }

        [Test]
        public void Deserialize_RejectsAnUndefinedEnumValueInsteadOfRestoringIt()
        {
            // Proves the typed reader is wired into the decoder, not merely present. Before this
            // milestone the cast at ReadParticipants accepted any int, so a team restored with a
            // side of 9999 and the failure, if it came at all, came later and wearing the wrong
            // reason.
            var fixture = CreateFixture();
            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var corrupted = RewriteFirstTeamSide(payload, 9999);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), corrupted, fixture.Configuration.CardDefinitions,
                    new[] { fixture.Board.MapDefinition }));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnrecognizedEnumValue),
                "An undefined enum is a distinct failure from a garbled byte stream; a resynchronizing client needs to tell them apart.");
            Assert.That(exception.Message, Does.Contain("TeamSide").And.Contains("9999"));
        }

        /// <summary>
        /// Milestone 6.2c closing check for finding F62B-04: restore re-derives a paused
        /// capability's candidates against the same resolved input bindings live execution
        /// supplied, rebuilt from the persisted completed outcomes. The capability lives in a
        /// test-local catalog so the registered set the compatibility corpus pins does not move;
        /// the pending frame is injected through the dispatcher's own public restore seam because
        /// the live composition roots name only the registered catalog.
        /// </summary>
        [Test]
        public void RoundTrip_RederivesPausedCapabilityCandidatesFromPersistedInputBindings()
        {
            var participants = StagedBoardParticipants.Create();
            var redHero = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blueHero = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var redDefinition = CreateInputDependentSkillDefinition();
            var blueDefinition = CreateMinimalMovementDefinition("test-input-blue", 3);
            var definitions = new[] { redDefinition, blueDefinition };
            var redCard = CardInstance.CreateInHand(
                new CardInstanceId("test-input-red-card"), redDefinition.Reference, redHero);
            var blueCard = CardInstance.CreateInHand(
                new CardInstanceId("test-input-blue-card"), blueDefinition.Reference, blueHero);
            var instances = new[] { redCard, blueCard };
            var catalog = new EffectCapabilityCatalog(RegisteredHandlers()
                .Concat(new IEffectCapabilityHandler[]
                {
                    new SeedSupplyHandler(),
                    new InputDependentAskerHandler()
                }));

            var live = EffectResolutionSession.Start(
                new EffectResolutionId("input-binding-resolution"),
                redCard.Id,
                redDefinition.Reference,
                new ActionDefinitionId("primary-skill"),
                redHero,
                redHero,
                ActionInvocationContext.PrintedPrimary,
                new EffectResolutionContext(
                    redDefinition.ActionDefinitions.Single().EffectRoot,
                    catalog,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill));
            Assert.That(live.Accepted, Is.True);
            Assert.That(live.Frame, Is.Not.Null, "The asker capability must pause the resolution.");
            var liveCandidates = live.Frame.PendingChoice.Candidates
                .Select(candidate => candidate.ToString()).ToArray();
            Assert.That(liveCandidates, Is.EqualTo(new[] { EffectValue.Hero(blueHero).ToString() }),
                "The live candidate set must come from the bound input, or this test pins nothing.");

            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) },
                new[]
                {
                    new HeroOccupancy(redHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueHero, new HexCoordinate(1, 0))
                });
            var contentCatalogs = new MatchContentCatalogs(
                catalog, MatchContentCatalogs.ForThisBuild().Components);
            var cardActions = CardActionResolutionAuthority.Restore(
                contentCatalogs,
                board,
                CardActionResolutionRestoreState.WithPendingSkill(
                    live.Frame,
                    HeroCoinLedgerRestoreStateHolder.Absent()),
                null,
                null,
                null,
                definitions,
                instances,
                participants,
                new MarkerAttachmentRegister(), RuleModificationSet.Empty);
            var round = new RoundResolutionAuthority(
                definitions,
                instances,
                participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));
            Assert.That(round.TryCommitCard(redHero, redCard.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blueHero, blueCard.Id).Accepted, Is.True);
            Assert.That(round.CurrentTurnAuthority.SelectedActor.CardInstanceId, Is.EqualTo(redCard.Id));
            Assert.That(round.CurrentTurnAuthority.PendingEffectChoice, Is.Not.Null);

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                contentCatalogs,
                payload,
                definitions,
                new[] { board.MapDefinition },
                Enumerable.Empty<QuickGameMinionSetupState>());

            var restoredChoice = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(
                restoredChoice.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(liveCandidates),
                "Restore must re-derive a paused capability's candidates against the same " +
                "resolved input bindings live execution supplied, rebuilt from the persisted " +
                "completed outcomes.");
        }

        private static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers()
        {
            var registered = SyntheticEffectCapabilities.CreateCatalog();
            return registered.Registered.Select(reference => registered.Load(reference)).ToArray();
        }

        private static CardDefinition CreateInputDependentSkillDefinition()
        {
            var root = new EffectNodeId("input-skill-root");
            var supply = new EffectNodeId("input-skill-supply");
            var ask = new EffectNodeId("input-skill-ask");
            var effect = new EffectDefinition(
                new EffectDefinitionId("test-input-dependent-skill"),
                "test-6-2c-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { supply, ask }),
                    new EffectNodeDefinition(
                        supply, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SeedSupplyHandler.SupplyReference,
                        outputs: new[] { new EffectOutputBinding("seed", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        ask, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        InputDependentAskerHandler.AskerReference,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "seed", supply, "seed", EffectValueKind.Hero)
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) })
                });
            return new CardDefinition(
                new CardDefinitionId("test-input-dependent-card"),
                "test-6-2c-rev-1",
                "TEST - Input Dependent Skill",
                new InitiativeValue(14),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        private static CardDefinition CreateMinimalMovementDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "test-6-2c-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });
        }

        /// <summary>Publishes the other hero as the seed the asker's candidates depend on.</summary>
        private sealed class SeedSupplyHandler : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference SupplyReference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("test-seed-supply"), 1);

            public EffectCapabilityReference Reference => SupplyReference;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("seed", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var other = context.Participants.Heroes
                    .Single(hero => !hero.Id.Equals(context.ActorId)).Id;
                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("seed", EffectValue.Hero(other)) });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Offers the bound input when it arrived and the actor when it did not, so a restore that
        /// drops the binding is observable as a different offered candidate set.
        /// </summary>
        private sealed class InputDependentAskerHandler : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference AskerReference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("test-input-dependent-asker"), 1);

            public EffectCapabilityReference Reference => AskerReference;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                parameters: new[] { new EffectCapabilityParameter("seed", EffectValueKind.Hero, false) },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var seed = context.Input("seed");
                var candidate = seed.IsDefined ? seed : EffectValue.Hero(context.ActorId);
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, new[] { candidate }, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("chosen", selected[0]) });

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.NeedsChoice;
        }

        private static SnapshotFixture CreateFixture()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            var blueHero = configuration.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(0, 1), new HexCoordinate(-1, 1) },
                new[] { new HeroOccupancy(redHero, new HexCoordinate(0, 0)), new HeroOccupancy(blueHero, new HexCoordinate(1, 0)) },
                new[] { new GuardsDigital.Core.Obstacles.ObstacleOccupancy(new GuardsDigital.Core.Obstacles.ObstacleId("snapshot-rock"), new HexCoordinate(-1, 1)) });
            var cardActions = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));
            return new SnapshotFixture(configuration, board, round);
        }

        /// <summary>
        /// Commits Red Pursuit at Initiative 7 against Blue Hammer at 6, so the Red hero acts first
        /// with a printed non-Ranged Attack. The showcase conversions took the Attack off the cards
        /// that used to sit at index 0 of each hand; these two are the pair that still carries one.
        /// </summary>
        private static void CommitFirstCards(SnapshotFixture fixture)
        {
            var red = fixture.Configuration.RedHand.Instances[3];
            var blue = fixture.Configuration.BlueHand.Instances[3];
            Assert.That(fixture.Round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            Assert.That(fixture.Round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn));
        }

        private static void CompleteRoundWithHolds(
            RoundResolutionAuthority round,
            MatchConfiguration configuration)
        {
            for (var turn = 0; turn < RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                var red = configuration.RedHand.Instances[turn];
                var blue = configuration.BlueHand.Instances[turn];
                Assert.That(round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
                while (round.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    // Milestone 8.35g. The release sits ABOVE the read of the selection below it,
                    // because acknowledging the resolved card is what empties that selection.
                    if (round.CurrentTurnAuthority.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    Assert.That(round.TryPerformHold(round.CurrentTurnAuthority.SelectedActor.CardInstanceId).Accepted, Is.True);
                }
            }
        }

        private static string CardFact(CardInstance card)
        {
            return $"{card.Id.Value}|{card.DefinitionReference}|{card.OwnerId.Value}|" +
                $"{card.State.Location}|{card.State.HasBeenPlayed}|{card.State.ResolutionStatus}|{card.State.FaceOrientation}";
        }

        private static string[] EventFacts(System.Collections.Generic.IEnumerable<RoundResolutionEvent> events)
        {
            return events.Select(value => $"{value.RoundIndex}:{value.TurnIndex}:{value.Kind}:" +
                $"{value.TurnEvent.Kind}:{value.TurnEvent.CardActionEvent.Kind}").ToArray();
        }

        private static string[] SlotFacts(System.Collections.Generic.IEnumerable<RoundTurnSlot> slots)
        {
            return slots.Select(slot => $"{slot.RoundIndex}:{slot.TurnIndex}:" + string.Join(",",
                slot.Assignments.Select(assignment => $"{assignment.HeroId.Value}=" +
                    (assignment.CardInstanceId?.Value ?? "PASS")))).ToArray();
        }

        /// <summary>
        /// Overwrites the first serialized <c>TeamSide</c>, located by walking the payload prefix
        /// exactly as <c>Serialize</c> laid it down - magic, schema version, the definition table,
        /// the team count, the first team's id - rather than by a hard-coded offset that a later
        /// section change would silently invalidate. Same idea as
        /// <see cref="RewriteSchemaVersion"/>, one section further in.
        /// </summary>
        private static string RewriteFirstTeamSide(string payload, int side)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                {
                    reader.ReadString();
                    reader.ReadInt32();
                    var definitionCount = reader.ReadInt32();
                    for (var index = 0; index < definitionCount; index++)
                    {
                        reader.ReadString();
                        reader.ReadString();
                    }

                    // Milestone 6.2a inserted the capability-registration section between the definition
                    // table and the participants. Walked, not skipped by offset, for the same reason
                    // this whole helper replays the writer's layout: a fixed offset would silently
                    // point at the wrong field the next time a section moves.
                    var capabilityCount = reader.ReadInt32();
                    for (var index = 0; index < capabilityCount; index++)
                    {
                        reader.ReadString();
                        reader.ReadInt32();
                    }

                    // Milestone 6.2 inserted the component registrations - tokens then markers - in the
                    // same place and for the same reason, so they are walked the same way.
                    for (var section = 0; section < 2; section++)
                    {
                        var componentCount = reader.ReadInt32();
                        for (var index = 0; index < componentCount; index++)
                        {
                            reader.ReadString();
                            reader.ReadInt32();
                        }
                    }

                    var teamCount = reader.ReadInt32();
                    Assert.That(teamCount, Is.GreaterThan(0), "The fixture must serialize at least one team.");
                    reader.ReadString();
                }

                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    writer.Write(side);
            }

            return Convert.ToBase64String(bytes);
        }

        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                    reader.ReadString();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    writer.Write(schemaVersion);
            }
            return Convert.ToBase64String(bytes);
        }

        private sealed class SnapshotFixture
        {
            public SnapshotFixture(MatchConfiguration configuration,
                AdjacentMoveAuthority board, RoundResolutionAuthority round)
            { Configuration = configuration; Board = board; Round = round; }
            public MatchConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public RoundResolutionAuthority Round { get; }
        }
    }
}
