using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5's three placement capabilities, executed as the production handlers the catalog
    /// registers: the actor's-space bridge, p.18's Place, and p.18's Spawn.
    ///
    /// The board is the official map wherever a Spawn point is implicated, because a Spawn point is map
    /// content and a synthetic coordinate board declares none - which is also why these cases are
    /// meaningful: they run against the geography the game is actually played on.
    /// </summary>
    public sealed class PlacementCapabilityTests
    {
        private static readonly HeroId RedActor =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId BlueActor =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        private static readonly HexCoordinate RedHeroAt = new HexCoordinate(-2, -5);

        // ---------------------------------------------------------------------------------------
        // The actor's-space bridge
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheActorSpaceBridge_PublishesTheActingHerosCoordinateWithoutAskingAnything()
        {
            var board = CreateOfficialBoard();

            var applied = Handler(SyntheticPlacementCapabilities.ActorSpaceV1)
                .Execute(Context(board, RedActor));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Where the actor stands is a fact, not a decision, so no prompt is invented for it.");
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "space").Value.Coordinate,
                Is.EqualTo(RedHeroAt));
        }

        [Test]
        public void TheActorSpaceBridge_ReportsItselfUnableToActWithoutABoard()
        {
            var applied = Handler(SyntheticPlacementCapabilities.ActorSpaceV1)
                .Execute(BoardlessContext());

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A hero that stands nowhere is a different answer from standing somewhere dull.");
        }

        // ---------------------------------------------------------------------------------------
        // Place
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Place_OffersEveryEmptyPlayableSpaceInRadiusExceptTheSubjectsOwn()
        {
            var board = CreateOfficialBoard();

            var offered = Handler(SyntheticPlacementCapabilities.PlaceV1)
                .Execute(Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 2)));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "The destination is chooser-owned; the resolver never picks a space for a player.");
            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Has.No.Member(RedHeroAt), "p.18 requires a different space.");
            Assert.That(spaces, Is.All.Matches<HexCoordinate>(board.IsEmptyPlayableSpace));
            Assert.That(
                spaces.All(space => HexDistanceQuery.Calculate(RedHeroAt, space) <= 2), Is.True,
                "The declared radius is inclusive and measured as rulebook distance.");
        }

        [Test]
        public void Place_RelocatesTheNamedPieceAndPublishesTheSpaceItLandedOn()
        {
            var board = CreateOfficialBoard();
            var destination = Handler(SyntheticPlacementCapabilities.PlaceV1)
                .Execute(Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 2)))
                .Choice.Candidates.Last().Coordinate;
            Assert.That(HexDistanceQuery.Calculate(RedHeroAt, destination), Is.EqualTo(2),
                "The furthest offered space is a genuine radius-2 placement, not an adjacent step.");

            var applied = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 2)),
                new[] { EffectValue.Space(destination) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "placed").Value.Coordinate,
                Is.EqualTo(destination));
            Assert.That(Coordinate(board, RedActor), Is.EqualTo(destination));
            Assert.That(board.IsEmptyPlayableSpace(RedHeroAt), Is.True);
            Assert.That(
                applied.Outcomes.Select(outcome => outcome.OutputName), Is.EqualTo(new[] { "placed" }),
                "Placing is not movement (p.18), so the only fact published names the placement.");
        }

        [Test]
        public void Place_TakesAnyGamePieceItIsNamed_IncludingATokenAndAMinion()
        {
            // p.18's own wording is "a game piece (such as a miniature, or a token)", so the general
            // Place declares the unrestricted kind and a narrower rule states its own.
            var board = CreateOfficialBoard();
            var tokenAt = board.MapDefinition.PlayableCoordinates
                .Where(board.IsEmptyPlayableSpace)
                .OrderBy(space => HexDistanceQuery.Calculate(RedHeroAt, space))
                .First();
            PlaceToken(board, tokenAt);
            var minion = board.Minions.First();
            var tokenDestination = Handler(SyntheticPlacementCapabilities.PlaceV1)
                .Execute(Context(board, RedActor, PlaceInputs(tokenAt, radius: 2)))
                .Choice.Candidates[0].Coordinate;

            var token = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(tokenAt, radius: 2)),
                new[] { EffectValue.Space(tokenDestination) });
            var minionDestination = Handler(SyntheticPlacementCapabilities.PlaceV1)
                .Execute(Context(board, RedActor, PlaceInputs(minion.Coordinate, radius: 1)))
                .Choice.Candidates[0].Coordinate;
            var moved = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(minion.Coordinate, radius: 1)),
                new[] { EffectValue.Space(minionDestination) });

            Assert.That(token.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(tokenDestination));
            Assert.That(moved.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.Minions.Single(candidate => candidate.MinionId.Equals(minion.MinionId)).Coordinate,
                Is.EqualTo(minionDestination));
        }

        [Test]
        public void Place_RefusesAStaleOrIllegalDestinationAgainstAFreshDerivation()
        {
            var board = CreateOfficialBoard();

            var occupied = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 2)),
                new[] { EffectValue.Space(board.Minions.First().Coordinate) });
            var terrain = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 8)),
                new[] { EffectValue.Space(new HexCoordinate(1, -5)) });
            var wrongKind = Handler(SyntheticPlacementCapabilities.PlaceV1).Resume(
                Context(board, RedActor, PlaceInputs(RedHeroAt, radius: 2)),
                new[] { EffectValue.Hero(RedActor) });

            Assert.That(occupied.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(terrain.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "q1_r-5 is a declared terrain cell, so nothing may be placed there.");
            Assert.That(wrongKind.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(Coordinate(board, RedActor), Is.EqualTo(RedHeroAt), "Nothing moved.");
        }

        [Test]
        public void Place_ReportsNoDestinationTheWayItsOwnNodePolicyDeclaresIt()
        {
            // The no-destination arm is read from the node's failure policy rather than a parameter of
            // its own, which is the decision milestone 6.4 recorded for distance optionality: p.10
            // already carries the mandatory-versus-optional distinction there.
            var board = CreateBoxedBoard();

            var mandatory = Handler(SyntheticPlacementCapabilities.PlaceV1).Execute(
                Context(board, RedActor, PlaceInputs(new HexCoordinate(0, 0), radius: 1)));
            var ifAble = Handler(SyntheticPlacementCapabilities.PlaceV1).Execute(
                Context(board, RedActor, PlaceInputs(new HexCoordinate(0, 0), radius: 1),
                    EffectFailurePolicy.IfAble));

            Assert.That(mandatory.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(ifAble.Kind, Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "`if able` makes having nowhere to put it a skipped step rather than a stopped action.");
        }

        // ---------------------------------------------------------------------------------------
        // Spawn
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheUnitSupply_IsEmptyWhileEveryDeclaredMiniatureStands()
        {
            var board = CreateOfficialBoard();

            Assert.That(board.FindUnitSupply(), Is.Empty,
                "p.18's Respawn states the ordering the supply follows: a minion must first be " +
                "removed before it can come back.");
            Assert.That(
                Handler(SyntheticPlacementCapabilities.SpawnV1).Execute(Context(board, RedActor)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void Spawn_OffersOnlyTheActingTeamsSupplyMiniatures()
        {
            var board = CreateOfficialBoard("red-melee-01", "blue-melee-01");

            var offered = Handler(SyntheticPlacementCapabilities.SpawnV1)
                .Execute(Context(board, RedActor));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.MinionId.Value),
                Is.EqualTo(new[] { "red-melee-01" }),
                "Offering the acting hero's own team is this fixture's synthetic bound, the way the " +
                "adjacency bound is Object Push's; the supply itself holds both.");
            Assert.That(
                board.FindUnitSupply().Select(unit => unit.MinionId.Value),
                Is.EqualTo(new[] { "blue-melee-01", "red-melee-01" }));
        }

        [Test]
        public void Spawn_PutsTheChosenMiniatureOnItsOwnDeclaredSpawnPoint()
        {
            var board = CreateOfficialBoard("red-melee-01");
            var reserved = board.FindUnitSupply().Single();
            Assert.That(board.TryGetSpawnPointCoordinate(reserved.SpawnPoint, out var spawnAt), Is.True);

            var applied = Handler(SyntheticPlacementCapabilities.SpawnV1).Resume(
                Context(board, RedActor), new[] { EffectValue.Minion(reserved.MinionId) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "spawned").Value.MinionId,
                Is.EqualTo(reserved.MinionId));
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "at").Value.Coordinate,
                Is.EqualTo(spawnAt));
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(reserved.MinionId)).Coordinate,
                Is.EqualTo(spawnAt));
            Assert.That(board.FindUnitSupply(), Is.Empty, "The supply is derived, so it simply shrank.");
        }

        [Test]
        public void Spawn_RemovesACoveringTokenFirstAndThenSpawnsTheUnit()
        {
            // p.19: "If a unit Spawns in a Spawn Point covered by a token, remove the token first,
            // then spawn the unit." The clearance is the Spawn's own first step, inside one commit.
            var board = CreateOfficialBoard("red-melee-01");
            var reserved = board.FindUnitSupply().Single();
            Assert.That(board.TryGetSpawnPointCoordinate(reserved.SpawnPoint, out var spawnAt), Is.True);
            PlaceToken(board, spawnAt);

            var offered = Handler(SyntheticPlacementCapabilities.SpawnV1)
                .Execute(Context(board, RedActor));
            var applied = Handler(SyntheticPlacementCapabilities.SpawnV1).Resume(
                Context(board, RedActor), new[] { EffectValue.Minion(reserved.MinionId) });

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "A token-covered Spawn point stays a legal destination, because p.19 clears it.");
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens, Is.Empty, "The covering token went back to its supply.");
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(reserved.MinionId)).Coordinate,
                Is.EqualTo(spawnAt));
        }

        [Test]
        public void Spawn_RefusesASpawnPointHeldByAnObstacleOtherThanAToken()
        {
            var board = CreateOfficialBoard("red-melee-01");
            var reserved = board.FindUnitSupply().Single();
            Assert.That(board.TryGetSpawnPointCoordinate(reserved.SpawnPoint, out var spawnAt), Is.True);
            var blocker = board.Minions.First(minion => minion.TeamSide == TeamSide.Red);
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForMinion(blocker.MinionId), spawnAt).Accepted,
                Is.True,
                "Fixture precondition: another miniature now stands on that Spawn point.");

            var offered = Handler(SyntheticPlacementCapabilities.SpawnV1)
                .Execute(Context(board, RedActor));
            var forced = board.CommitValidatedUnitSpawn(reserved.MinionId);

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.19 admits no second obstacle in a space, and no rule clears a miniature away.");
            Assert.That(forced.Accepted, Is.False);
            Assert.That(forced.RejectionReason, Is.EqualTo(UnitSpawnRejectionReason.SpawnPointOccupied));
        }

        [Test]
        public void Spawn_RefusesAMiniatureThatIsNoLongerInTheSupply()
        {
            var board = CreateOfficialBoard("red-melee-01");
            var reserved = board.FindUnitSupply().Single();
            Assert.That(board.CommitValidatedUnitSpawn(reserved.MinionId).Accepted, Is.True);

            var stale = Handler(SyntheticPlacementCapabilities.SpawnV1).Resume(
                Context(board, RedActor), new[] { EffectValue.Minion(reserved.MinionId) });
            var again = board.CommitValidatedUnitSpawn(reserved.MinionId);

            Assert.That(stale.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The supply is re-derived on the answer, not remembered from the offer.");
            Assert.That(again.RejectionReason, Is.EqualTo(UnitSpawnRejectionReason.UnitAlreadyOnBoard));
            Assert.That(
                board.CommitValidatedUnitSpawn(new MinionId("no-such-minion")).RejectionReason,
                Is.EqualTo(UnitSpawnRejectionReason.UnknownUnit));
        }

        [Test]
        public void Spawn_ReportsItselfUnableToActWithoutTheUnitPort()
        {
            var applied = Handler(SyntheticPlacementCapabilities.SpawnV1).Execute(BoardlessContext());

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticEffectCapabilities.CreateCatalog().Load(reference);

        private static Dictionary<string, EffectValue> PlaceInputs(HexCoordinate subject, int radius) =>
            new Dictionary<string, EffectValue>
            {
                ["subject"] = EffectValue.Space(subject),
                ["radius"] = EffectValue.Integer(radius)
            };

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HeroId actor,
            IReadOnlyDictionary<string, EffectValue> inputs = null,
            EffectFailurePolicy failurePolicy = EffectFailurePolicy.Mandatory) =>
            new EffectCapabilityContext(
                new EffectNodeId("placement-node"),
                failurePolicy,
                actor,
                actor,
                new CardInstanceId("placement-card"),
                new ActionDefinitionId("placement-action"),
                inputs ?? new Dictionary<string, EffectValue>(),
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(),
                    board, board, board));

        private static EffectCapabilityContext BoardlessContext() =>
            new EffectCapabilityContext(
                new EffectNodeId("placement-node"),
                EffectFailurePolicy.Mandatory,
                RedActor,
                RedActor,
                new CardInstanceId("placement-card"),
                new ActionDefinitionId("placement-action"),
                new Dictionary<string, EffectValue>(),
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), null, new MarkerAttachmentRegister(),
                    null, null, null));

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static void PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"placement-token-{coordinate.Q}-{coordinate.R}"),
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.HazardV3),
                RedActor,
                RedActor,
                new CardInstanceId("placement-token-source"),
                coordinate));

        /// <summary>
        /// The official map with the configured setup, optionally holding some declared miniatures in
        /// the supply instead of on the board. The supply is reached through a restore because that is
        /// how the engine reaches it without playing a whole round: a restored position list that does
        /// not account for a declared miniature is exactly a miniature that has left the board.
        /// </summary>
        private static AdjacentMoveAuthority CreateOfficialBoard(params string[] reservedMinionIds)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var heroes = setup.Heroes
                .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))
                .ToArray();
            var placed = new AdjacentMoveAuthority(
                map, heroes, Array.Empty<ObstacleOccupancy>(), setup.Minions);
            if (reservedMinionIds.Length == 0)
            {
                return placed;
            }

            var state = placed.CaptureRestoreState();
            return AdjacentMoveAuthority.Restore(
                new BoardRestoreState(
                    state.HeroOccupancies,
                    state.ObstacleOccupancies,
                    state.RemainingMinions.Where(position =>
                        !reservedMinionIds.Contains(position.MinionId.Value)),
                    state.PlacedTokens),
                map,
                setup.Minions);
        }

        /// <summary>A single-space board, so a radius-1 Place has nowhere at all to put anything.</summary>
        private static AdjacentMoveAuthority CreateBoxedBoard() =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("synthetic-boxed-place-map"), 1, new[] { new HexCoordinate(0, 0) }),
                new[] { new HeroOccupancy(RedActor, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
    }
}
