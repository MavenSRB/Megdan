using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5 end to end through <see cref="IMatchSession"/>: p.18's Place and Spawn as
    /// authoritative commands on the official map, with atomic rejection, snapshot restore, replay, and
    /// retained-prefix Undo.
    ///
    /// Board state is read back by deserializing the authoritative snapshot, as milestone 6.2's
    /// component fixture and 6.4's displacement fixture both do, because this milestone's non-goals bar
    /// a placement-specific projection property.
    /// </summary>
    public sealed class MatchSessionPlacementDispatchTests
    {
        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("placement-place-branch");
        private static readonly EffectNodeId SpawnBranch = new EffectNodeId("placement-spawn");

        private static readonly ActionDefinitionId PlacementSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-3, 10);

        [Test]
        public void ThePlaceBranch_OffersTheActorsOwnRadiusWithoutAskingWhichPieceMoves()
        {
            var session = StartPlacementSkill();

            ChooseBranch(session, PlaceBranch);

            var destinations = session.Projection.PendingEffectChoice;
            Assert.That(destinations, Is.Not.Null);
            Assert.That(destinations.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget),
                "The destination is the branch's one decision: `Place your hero` names the piece, so " +
                "the actor's own space arrives as a published fact rather than a one-option prompt.");
            var offered = destinations.Candidates.Select(candidate => candidate.Value.Coordinate).ToArray();
            Assert.That(offered, Has.No.Member(RedAt), "p.18 requires a different space.");
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(RedAt, space) <= 2), Is.True,
                "Radius 2 is inclusive rulebook distance, counted through obstacles rather than walked.");
        }

        [Test]
        public void ThePlaceBranch_MovesTheActorWithoutEmittingAMovementOrFastTravelFact()
        {
            var session = StartPlacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;
            ChooseBranch(session, PlaceBranch);
            var destination = session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate;

            SelectValue(session, destination);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(destination));
            Assert.That(board.IsEmptyPlayableSpace(RedAt), Is.True,
                "The vacated space is free for every occupancy consumer.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "Placing is not movement (p.18): no Movement action was started and none is pending.");
        }

        [Test]
        public void AnIllegalPlaceDestination_IsRejectedAtomicallyAndThePendingChoiceSurvives()
        {
            var session = StartPlacementSkill();
            ChooseBranch(session, PlaceBranch);

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(new HexCoordinate(1, -5))))));

            Assert.That(illegal.Accepted, Is.False,
                "q1_r-5 is terrain, and no space the query never offered can be answered with.");
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal is atomic: the pending placement is exactly what it was.");
        }

        [Test]
        public void APausedPlaceChoice_SurvivesSnapshotRestoreWithRederivedCandidates()
        {
            var session = StartPlacementSkill();
            ChooseBranch(session, PlaceBranch);
            var paused = session.Projection.PendingEffectChoice;

            var byteIdentical = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(byteIdentical.Round, byteIdentical.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A paused placement round-trips byte-identically.");

            var payload = session.CaptureSnapshot();
            var restored = StartPlacementSkill();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            Assert.That(
                restored.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer.");
            SelectValue(restored, paused.Candidates[0].Value.Coordinate);
            Assert.That(restored.Projection.PendingSkillResolution, Is.Null,
                "The restored placement continues to the same completion the live one would.");
        }

        [Test]
        public void APlace_ReplaysAndUndoesDeterministically()
        {
            var session = StartPlacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;
            ChooseBranch(session, PlaceBranch);
            var destination = session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate;

            var selection = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(destination));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(destination));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(RedAt),
                "Retained-prefix Undo puts the placed hero back, because a Place is authoritative " +
                "board state the replay reaches.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void TheSpawnBranch_StopsTheActionWhileEveryDeclaredMiniatureStands()
        {
            // p.18's Spawn takes only from the supply, and at setup the supply is empty because every
            // declared miniature is on the board. A mandatory step that cannot happen stops the action.
            var session = StartPlacementSkill();

            ChooseBranch(session, SpawnBranch);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(RestoreBoard(session).Minions.Count, Is.EqualTo(12), "Nothing entered the board.");
        }

        [Test]
        public void TheSpawnBranch_PutsARemovedMiniatureBackOnItsOwnSpawnPoint()
        {
            var session = StartPlacementSkillAfterAMinionBattleRemoval(out var removed);
            var board = RestoreBoard(session);
            Assert.That(
                board.FindUnitSupply().Select(unit => unit.MinionId), Has.Member(removed),
                "The Minion Battle put a miniature back into the supply, which is where a Spawn " +
                "takes from.");
            Assert.That(board.TryGetSpawnPointCoordinate(
                board.FindUnitSupply().Single(unit => unit.MinionId.Equals(removed)).SpawnPoint,
                out var spawnAt), Is.True);

            ChooseBranch(session, SpawnBranch);
            var offered = session.Projection.PendingEffectChoice;
            Assert.That(offered, Is.Not.Null);
            Assert.That(
                offered.Candidates.Select(candidate => candidate.Value.MinionId), Has.Member(removed),
                "Which miniature Spawns is the branch's one decision.");
            Accept(session, DirectorCommand.AnswerEffectChoice(offered.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.MinionId.Equals(removed))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var after = RestoreBoard(session);
            Assert.That(
                after.Minions.Single(minion => minion.MinionId.Equals(removed)).Coordinate,
                Is.EqualTo(spawnAt),
                "p.19: a Spawn point is the space a minion Spawns onto.");
            Assert.That(after.FindUnitSupply().Select(unit => unit.MinionId), Has.No.Member(removed));
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void ASpawn_SurvivesSnapshotRestoreAndUndoesDeterministically()
        {
            var session = StartPlacementSkillAfterAMinionBattleRemoval(out var removed);
            ChooseBranch(session, SpawnBranch);
            var selection = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.MinionId.Equals(removed));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            var restored = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A committed Spawn round-trips with no field the writer and reader disagree on.");
            Assert.That(
                restored.Board.FindUnitSupply().Select(unit => unit.MinionId), Has.No.Member(removed),
                "The supply is rebuilt from the configured roster minus the restored positions, so a " +
                "Spawn narrows it without anything about it reaching the wire.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(
                RestoreBoard(session).FindUnitSupply().Select(unit => unit.MinionId), Has.Member(removed),
                "Retained-prefix Undo puts the miniature back in the supply.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void NoOrdinaryConfiguredCard_ExposesAPlacementCapability()
        {
            // The showcase conversions expose Place from a card a player holds at milestone 6.5a; until
            // then the ordinary hand must not reach these capabilities at all.
            var ordinary = PlaceholderOneVsOneTestConfiguration.Create();

            var placementNodes = ordinary.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node =>
                    node.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticPlacementCapabilities.PlaceV1) ||
                    node.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticPlacementCapabilities.SpawnV1) ||
                    node.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticPlacementCapabilities.ActorSpaceV1))
                .ToArray();

            Assert.That(placementNodes, Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static void ChooseBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.Coordinate.Equals(coordinate))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession StartPlacementSkill()
        {
            var session = CreatePlacementSession(QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne());
            CommitShoveCards(session);
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// A session that has played one round to its Minion Battle and removed a Red miniature, so the
        /// supply a Spawn draws from is not empty. Red is the losing side by construction - its setup
        /// declares one fewer miniature - because p.8 has the losing team remove theirs, and the Red
        /// hero is the one whose Skill this fixture reaches.
        /// </summary>
        private static MatchSession StartPlacementSkillAfterAMinionBattleRemoval(out MinionId removed)
        {
            var session = CreatePlacementSession(RedOutnumberedSetup());
            AdvanceToEndOfRound(session);
            Accept(session, DirectorCommand.AdvanceEndOfRound());

            var battle = session.Projection.PendingMinionBattleChoice;
            Assert.That(battle, Is.Not.Null, "The imbalance must reach a real removal choice.");
            var invocation = battle.SelectInvocations[0];
            removed = invocation.MinionId;
            Accept(session, DirectorCommand.SelectMinionBattleRemoval(invocation));
            Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);

            while (session.Projection.Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }

            CommitShoveCards(session);
            StartTheSkill(session);
            return session;
        }

        private static void StartTheSkill(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(PlacementSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(PlacementSkill),
                "The board-object Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitOrdinaryCards(session);
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        /// <summary>Each side's lowest-Initiative ordinary card, keeping both shove cards in hand.</summary>
        private static void CommitOrdinaryCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = EligibleCards(session, side)
                    .Where(candidate => !candidate.Id.Value.Contains("shove"))
                    .OrderBy(candidate => Initiative(session, candidate))
                    .First();
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static void CommitShoveCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = EligibleCards(session, side)
                    .Single(candidate => candidate.Id.Value.Contains("shove"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards
                .Where(value => value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand)
                .ToArray();
        }

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(value => value.Reference.Equals(card.DefinitionReference)).Initiative.Value;

        private static QuickGameSetupConfiguration RedOutnumberedSetup()
        {
            var full = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var dropped = full.Minions.First(value =>
                value.TeamSide == TeamSide.Red && value.MinionType == StandardMinionType.Ranged);
            return new QuickGameSetupConfiguration(
                full.RequiredMap,
                full.WaveCount,
                full.LifeCountPerTeam,
                full.Heroes,
                full.Minions.Where(value => !value.MinionId.Equals(dropped.MinionId)));
        }

        private static MatchSession CreatePlacementSession(QuickGameSetupConfiguration configured)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                configured, configuration.Participants, map, configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, RedAt),
                    new HeroOccupancy(blueHeroId, BlueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
