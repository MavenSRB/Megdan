using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2 component identity/supply/occupancy checks and milestone 6.3's persistent
    /// property, source-hero-independent lifetime, and Spawn-point-clearance checks at the layer that
    /// owns them.
    ///
    /// Check 1 - tokens use the authoritative finite supply and occupy board spaces as obstacles - is
    /// asserted behaviourally rather than by reading a collection: a space holding a token refuses a
    /// move onto it and drops out of movement reachability, which is what "is an obstacle" means to
    /// every existing consumer. Check 2's structural half is asserted the same way in reverse, by
    /// showing a marker changes no board state at all.
    /// </summary>
    public sealed class TokenAndMarkerComponentTests
    {
        private static readonly HeroId ActorId = new HeroId("component-hero");

        [Test]
        public void APlacedToken_IsAnObstacleForMovementAndOccupancy()
        {
            var origin = new HexCoordinate(0, 0);
            var tokenSpace = new HexCoordinate(1, 0);
            var board = CreateBoard(origin);

            var reachableBefore = Reachable(board, origin);
            board.CommitValidatedTokenPlacement(CreateToken("token-1", tokenSpace));
            var reachableAfter = Reachable(board, origin);
            var move = board.TryCommit(new AdjacentMoveCommand(ActorId, origin, tokenSpace));

            Assert.That(reachableBefore.Contains(tokenSpace), Is.True,
                "The space must be reachable before the token, or the test proves nothing.");
            Assert.That(reachableAfter.Contains(tokenSpace), Is.False,
                "Rulebook p.19: all tokens are obstacles.");
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.RejectionReason, Is.EqualTo(AdjacentMoveRejectionReason.DestinationOccupied));
            Assert.That(board.IsEmptyPlayableSpace(tokenSpace), Is.False);
        }

        [Test]
        public void ATokenSpace_RefusesEveryOtherOccupantKind()
        {
            var tokenSpace = new HexCoordinate(1, 0);
            var board = CreateBoard(new HexCoordinate(0, 0));
            board.CommitValidatedTokenPlacement(CreateToken("token-1", tokenSpace));

            Assert.That(
                () => board.CommitValidatedTokenPlacement(CreateToken("token-2", tokenSpace)),
                Throws.InvalidOperationException,
                "One obstacle per space, ever, no exceptions (rulebook p.19).");
            Assert.That(
                () => new AdjacentMoveAuthority(
                    Coordinates(),
                    new[] { new HeroOccupancy(ActorId, tokenSpace) },
                    new[] { new ObstacleOccupancy(new ObstacleId("rock"), tokenSpace) }),
                Throws.ArgumentException,
                "The shared occupancy index owns the rule for every kind, not per kind.");
        }

        [Test]
        public void TokenSupply_IsFiniteAndDerivedFromWhatIsOnTheBoard()
        {
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            var reference = SyntheticComponentDefinitions.HazardV4;
            var board = CreateBoard(new HexCoordinate(0, 0));
            var supply = SyntheticComponentDefinitions.HazardDefinition.SupplyCount;

            Assert.That(catalog.CountAvailableTokens(reference, board.PlacedTokens), Is.EqualTo(supply));

            board.CommitValidatedTokenPlacement(CreateToken("token-1", new HexCoordinate(1, 0)));

            Assert.That(catalog.CountAvailableTokens(reference, board.PlacedTokens), Is.Zero);
            Assert.That(
                () => board.CommitValidatedTokenPlacement(CreateToken("token-2", new HexCoordinate(0, 1))),
                Throws.InvalidOperationException,
                "Rulebook p.19: tokens are intentionally limited.");

            var reclaimed = board.CommitValidatedTokenReclamation(new TokenInstanceId("token-1"));

            Assert.That(reclaimed.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(catalog.CountAvailableTokens(reference, board.PlacedTokens), Is.EqualTo(supply),
                "Reclaiming returns the token to the supply, because the supply is the total minus " +
                "what is on the board rather than a counter that could disagree with it.");
            Assert.That(board.IsEmptyPlayableSpace(new HexCoordinate(1, 0)), Is.True,
                "A reclaimed token releases its space.");
        }

        [Test]
        public void AToken_OutlivesTheHeroWhoPlacedIt()
        {
            var board = CreateBoard(new HexCoordinate(0, 0));
            var token = CreateToken("token-1", new HexCoordinate(1, 0));
            board.CommitValidatedTokenPlacement(token);

            Assert.That(token.OwnerHeroId, Is.EqualTo(ActorId),
                "The placing hero is recorded as provenance.");
            Assert.That(board.PlacedTokens.Single().Definition,
                Is.SameAs(SyntheticComponentDefinitions.HazardDefinition),
                "A placed token's meaning is the catalog's object, never a copy it carries. Rulebook " +
                "p.19 ties a token's properties to the token, and p.19 also states it is not removed " +
                "when the hero who placed it is defeated, so nothing here may act as a lifetime link.");

            var captured = board.CaptureRestoreState();
            var withoutSourceHero = AdjacentMoveAuthority.Restore(
                new BoardRestoreState(
                    Array.Empty<HeroOccupancy>(),
                    captured.ObstacleOccupancies,
                    captured.RemainingMinions,
                    captured.PlacedTokens,
                    captured.CurrentBattleZoneId,
                    captured.MinionBattleExclusions,
                    captured.LastCompletedMovementWalk,
                    captured.HeroBodySupplies),
                board.MapDefinition,
                Array.Empty<QuickGameMinionSetupState>());

            Assert.That(withoutSourceHero.TryGetHeroCoordinate(ActorId, out _), Is.False,
                "The placing hero is no longer on the board, as after Defeat.");
            Assert.That(withoutSourceHero.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(withoutSourceHero.TokenPropertySources, Has.Count.EqualTo(1),
                "Rulebook p.19: neither the token nor its property ends when its source hero leaves the board.");
        }

        [Test]
        public void TokenProperties_AreContinuousSourcesOnlyWhileTheirTokenIsPlaced()
        {
            var board = CreateBoard(new HexCoordinate(0, 0));
            var token = CreateToken("token-property", new HexCoordinate(1, 0));
            var activeEffects = new ActiveEffectLifecycle();

            board.CommitValidatedTokenPlacement(token);

            var source = board.TokenPropertySources.Single();
            Assert.That(source.Kind, Is.EqualTo(ContinuousRuleSourceKind.TokenProperty));
            Assert.That(source.RuleKind, Is.EqualTo("synthetic-hazard"));
            Assert.That(source.ApplicabilityKind, Is.EqualTo("while-token-remains-placed"));
            Assert.That(activeEffects.Effects, Is.Empty,
                "Rulebook p.19: token properties are not Active effects.");

            board.CommitValidatedTokenReclamation(token.Id);

            Assert.That(board.TokenPropertySources, Is.Empty,
                "The placed token, rather than a source card or hero, owns the property's lifetime.");
            Assert.That(activeEffects.Effects, Is.Empty);
        }

        [Test]
        public void UnitSpawnOnTokenCoveredSpawnPoint_RemovesTheTokenFirst()
        {
            var zone = new BoardMapZone(
                new ZoneId("spawn-zone"), "Spawn Zone", "synthetic", "red", false, false);
            var actorSpace = new BoardMapSpace(
                new SpaceId("actor"), new HexCoordinate(0, 0), BoardMapSpaceKind.Playable, zone.Id);
            var spawnSpace = new BoardMapSpace(
                new SpaceId("spawn"), new HexCoordinate(1, 0), BoardMapSpaceKind.Playable, zone.Id);
            var spawnPoint = new BoardMapSpawnPoint(
                new SpawnPointId("red-spawn"), spawnSpace.Id, "red", "hero");
            var map = new BoardMapDefinition(
                new MapId("token-spawn-test"),
                1,
                "Token Spawn Test",
                new[] { actorSpace, spawnSpace },
                new[] { zone },
                new[] { spawnPoint },
                Array.Empty<ZoneId>());
            var board = new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(ActorId, actorSpace.Coordinate) },
                Array.Empty<ObstacleOccupancy>());
            var token = CreateToken("spawn-cover", spawnSpace.Coordinate);
            board.CommitValidatedTokenPlacement(token);

            Assert.That(
                () => board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                    new ConfiguredSpawnPointReference(
                        new MapId("other-map"), spawnPoint.Id)),
                Throws.InvalidOperationException);
            Assert.That(
                () => board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                    new ConfiguredSpawnPointReference(
                        map.Id, new SpawnPointId("missing-spawn"))),
                Throws.InvalidOperationException);
            Assert.That(board.PlacedTokens.Single(), Is.SameAs(token),
                "An invalid Spawn reference must not consume the covering token.");

            var removed = board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                new ConfiguredSpawnPointReference(map.Id, spawnPoint.Id));

            Assert.That(removed, Is.SameAs(token));
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(board.IsEmptyPlayableSpace(spawnSpace.Coordinate), Is.True,
                "The Spawn owner receives an empty destination after the mandatory token-removal step.");
            Assert.That(SyntheticComponentDefinitions.CreateCatalog().CountAvailableTokens(
                SyntheticComponentDefinitions.HazardV4, board.PlacedTokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount));

            Assert.That(board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                new ConfiguredSpawnPointReference(map.Id, spawnPoint.Id)), Is.Null,
                "An uncovered Spawn point needs no token mutation.");
        }

        [Test]
        public void AMarker_IsAnAttachmentWithNoBoardPresence()
        {
            var board = CreateBoard(new HexCoordinate(0, 0));
            var markers = new MarkerAttachmentRegister();
            var occupiedBefore = board.PlacedTokens.Count;

            markers.Give(CreateMarker("marker-1", new HeroId("holder-hero")));

            Assert.That(markers.Attachments.Single().HolderHeroId, Is.EqualTo(new HeroId("holder-hero")));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(occupiedBefore),
                "Rulebook p.18: unlike tokens, markers are not placed on the board.");
            Assert.That(
                typeof(MarkerAttachment).GetProperties()
                    .Any(property => property.PropertyType == typeof(HexCoordinate)),
                Is.False,
                "A marker has no space to occupy, so the record of one must have nowhere to put a " +
                "coordinate. This is the structural half of acceptance check 2.");
        }

        [Test]
        public void AMarker_CarriesExplicitReturnStateAndReturnsToItsSupply()
        {
            var markers = new MarkerAttachmentRegister();
            var holder = new HeroId("holder-hero");
            var definition = SyntheticComponentDefinitions.FocusDefinition;

            Assert.That(markers.CountAvailable(definition), Is.EqualTo(definition.SupplyCount));

            markers.Give(CreateMarker("marker-1", holder));
            var attachment = markers.Attachments.Single();

            Assert.That(attachment.ReturnCondition, Is.EqualTo(MarkerReturnCondition.GeneralCleanup),
                "Rulebook p.8 returns every marker at end of round and p.13 returns every marker a " +
                "defeated hero holds; the attachment states that rather than leaving it implied.");
            Assert.That(attachment.OwnerHeroId, Is.EqualTo(ActorId));
            Assert.That(attachment.OwnerHeroId, Is.Not.EqualTo(attachment.HolderHeroId),
                "Owner and holder are separate links, because a marker is normally given to another " +
                "hero and must return to the correct supply.");
            Assert.That(markers.CountAvailable(definition), Is.Zero);
            Assert.That(
                () => markers.Give(CreateMarker("marker-2", holder)),
                Throws.InvalidOperationException,
                "The marker supply is finite in the same way the token supply is.");

            var returned = markers.Return(attachment.Id);

            Assert.That(returned.HolderHeroId, Is.EqualTo(holder));
            Assert.That(markers.Attachments, Is.Empty);
            Assert.That(markers.CountAvailable(definition), Is.EqualTo(definition.SupplyCount));
        }

        [Test]
        public void RestoredComponentState_RefusesMoreCopiesThanTheTrustedSupplyContains()
        {
            var overdrawn = new[]
            {
                CreateMarker("marker-1", new HeroId("holder-one")),
                CreateMarker("marker-2", new HeroId("holder-two"))
            };

            Assert.That(
                () => MarkerAttachmentRegister.Restore(new MarkerAttachmentRestoreState(overdrawn)),
                Throws.ArgumentException,
                "A payload cannot restore more markers than the catalog says exist; the supply is " +
                "content, so the trusted total wins over what the bytes claim.");
            Assert.That(
                () => new MarkerAttachmentRestoreState(new[]
                {
                    CreateMarker("marker-1", new HeroId("holder-one")),
                    CreateMarker("marker-1", new HeroId("holder-two"))
                }),
                Throws.ArgumentException,
                "Marker instance identity must stay unique across a restore.");
        }

        [Test]
        public void PrintedRetainedMarker_StaysBelowItsTurnSlotThroughBothGeneralCleanupCheckpoints()
        {
            var definition = new MarkerDefinition(
                new MarkerDefinitionId("test-persistent-slot-rune"), 1, "TEST - Rune", 1,
                MarkerReturnCondition.RetainedAtRoundEndAndDefeat);
            var marker = new MarkerAttachment(
                new MarkerInstanceId("slot-rune"), definition, ActorId, ActorId,
                default, default, 3);
            var markers = new MarkerAttachmentRegister();
            markers.Give(marker);

            Assert.That(marker.TurnSlotIndex, Is.EqualTo(3));
            Assert.That(ComponentCleanupRules.MarkersReturnedAtEndOfRound(markers.Attachments), Is.Empty);
            Assert.That(ComponentCleanupRules.MarkersReturnedOnDefeat(markers.Attachments, ActorId), Is.Empty);
            Assert.That(markers.CountAvailable(definition), Is.Zero);
            Assert.That(MarkerAttachmentRegister.Restore(markers.CaptureRestoreState())
                .Attachments.Single().TurnSlotIndex, Is.EqualTo(3));
            Assert.That(markers.Return(marker.Id), Is.SameAs(marker),
                "Printed retention does not prohibit an explicit return operation.");
        }

        [Test]
        public void TurnSlotMarkers_RejectOutOfRangeSlotsAndOrdinaryCleanupDefinitions()
        {
            var retained = new MarkerDefinition(
                new MarkerDefinitionId("test-persistent-slot-rune"), 1, "TEST - Rune", 1,
                MarkerReturnCondition.RetainedAtRoundEndAndDefeat);
            Assert.That(() => new MarkerAttachment(
                new MarkerInstanceId("zero"), retained, ActorId, ActorId,
                default, default, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MarkerAttachment(
                new MarkerInstanceId("five"), retained, ActorId, ActorId,
                default, default, 5), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MarkerAttachment(
                new MarkerInstanceId("ordinary"), SyntheticComponentDefinitions.FocusDefinition,
                ActorId, ActorId, default, default, 1), Throws.ArgumentException);
            Assert.That(() => new MarkerDefinition(
                new MarkerDefinitionId("unknown-return"), 1, "TEST - Unknown", 1,
                (MarkerReturnCondition)99), Throws.ArgumentException);
        }

        [Test]
        public void FourRunePlacement_IsAtomicAndPreservesPhysicalIdentityOnReinscription()
        {
            var definitions = Enumerable.Range(1, 4).Select(index => new MarkerDefinition(
                new MarkerDefinitionId("test-slot-rune-" + index), 1,
                "TEST - Rune " + index, 1,
                MarkerReturnCondition.RetainedAtRoundEndAndDefeat)).ToArray();
            MarkerAttachment[] Place(params int[] slots) => definitions.Select((definition, index) =>
                new MarkerAttachment(new MarkerInstanceId("rune-" + (index + 1)), definition,
                    ActorId, ActorId, default, default, slots[index])).ToArray();

            var register = new MarkerAttachmentRegister();
            register.SetTurnSlotMarkers(ActorId, Place(4, 1, 3, 2));
            Assert.That(register.Attachments.Select(value => value.TurnSlotIndex),
                Is.EquivalentTo(new int?[] { 1, 2, 3, 4 }));
            Assert.That(register.Attachments.Select(value => value.Id).Distinct().Count(), Is.EqualTo(4));

            var beforeInvalid = register.Attachments.ToArray();
            Assert.That(() => register.SetTurnSlotMarkers(ActorId, Place(1, 1, 3, 4)),
                Throws.ArgumentException);
            Assert.That(register.Attachments, Is.EqualTo(beforeInvalid),
                "An incomplete or duplicate placement must not partially move a rune.");

            register.SetTurnSlotMarkers(ActorId, Place(1, 2, 3, 4));
            Assert.That(register.Attachments.Select(value => value.Id.Value),
                Is.EquivalentTo(beforeInvalid.Select(value => value.Id.Value)),
                "Re-inscribing moves the four physical markers instead of minting four copies.");

            var stale = Place(4, 3, 2, 1);
            stale[0] = new MarkerAttachment(new MarkerInstanceId("fake-replacement"),
                definitions[0], ActorId, ActorId, default, default, 4);
            Assert.That(() => register.SetTurnSlotMarkers(ActorId, stale),
                Throws.InvalidOperationException);
            Assert.That(register.Attachments.Select(value => value.TurnSlotIndex),
                Is.EquivalentTo(new int?[] { 1, 2, 3, 4 }));
        }

        private static AdjacentMoveAuthority CreateBoard(HexCoordinate actorOrigin) =>
            new AdjacentMoveAuthority(
                Coordinates(),
                new[] { new HeroOccupancy(ActorId, actorOrigin) });

        private static HexCoordinate[] Coordinates()
        {
            var coordinates = new System.Collections.Generic.List<HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            {
                for (var r = Math.Max(-3, -q - 3); r <= Math.Min(3, -q + 3); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            return coordinates.ToArray();
        }

        /// <summary>
        /// The authority-backed reachability entry, which is the one the Movement backend itself calls.
        /// It reads the shared occupancy index rather than per-kind lists, so a token blocks transit
        /// without this query - or the backend, or the presentation - learning that tokens exist. That
        /// is the property milestone 6.1r's index was built to deliver and this milestone is the first
        /// to collect on.
        /// </summary>
        private static HexCoordinate[] Reachable(AdjacentMoveAuthority board, HexCoordinate origin) =>
            MovementReachabilityQuery
                .FindReachableDestinations(
                    new HeroOccupancy(ActorId, origin), board, 1, RuleModificationSet.Empty)
                .Select(destination => destination.Destination)
                .ToArray();

        private static PlacedToken CreateToken(string id, HexCoordinate coordinate) =>
            new PlacedToken(
                new TokenInstanceId(id),
                SyntheticComponentDefinitions.HazardDefinition,
                ActorId,
                ActorId,
                default,
                coordinate);

        private static MarkerAttachment CreateMarker(string id, HeroId holder) =>
            new MarkerAttachment(
                new MarkerInstanceId(id),
                SyntheticComponentDefinitions.FocusDefinition,
                ActorId,
                holder,
                default,
                default);
    }
}
