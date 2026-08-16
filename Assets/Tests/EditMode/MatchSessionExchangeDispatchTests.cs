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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6 end to end through <see cref="IMatchSession"/>: p.19's Swap and Remove as
    /// authoritative commands on the official map, with atomic rejection, snapshot restore, replay,
    /// retained-prefix Undo, and milestone 6.4a's after-action correction.
    ///
    /// The Replace branch is exercised at the capability and operation level instead, in
    /// <c>BoardObjectExchangeTests</c>. Reaching it from a session needs a Hazard token already
    /// standing beside the actor, and no initial scenario can seed one - a token arrives by being
    /// placed during play, from a different card's Skill. Nothing about the dispatch path differs
    /// between the three: all three pause through the same target selection, commit inside the same
    /// resolution, and store nothing of their own, which is exactly what these cases establish.
    ///
    /// Board state is read back by deserializing the authoritative snapshot, as milestone 6.2's
    /// component fixture and 6.4's and 6.5's do, because this milestone's non-goals bar an
    /// exchange-specific projection property.
    /// </summary>
    public sealed class MatchSessionExchangeDispatchTests
    {
        private static readonly EffectNodeId SwapBranch = new EffectNodeId("exchange-swap-branch");
        private static readonly EffectNodeId RemoveBranch = new EffectNodeId("exchange-remove-branch");
        private static readonly EffectNodeId ReplaceBranch = new EffectNodeId("exchange-replace-branch");

        private static readonly ActionDefinitionId ExchangeSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly HexCoordinate BlueAt = new HexCoordinate(-3, 10);

        [Test]
        public void TheSwapBranch_ExchangesTheActorWithTheAdjacentObjectItChose()
        {
            var session = StartExchangeSkill(out var actorAt, out var minion, out var minionAt);
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseBranch(session, SwapBranch);
            SelectValue(session, minionAt);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(minionAt));
            Assert.That(
                board.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate,
                Is.EqualTo(actorAt),
                "p.19: two objects swap places with each other, in one transaction.");
            Assert.That(board.Minions.Count, Is.EqualTo(12), "Nothing left the board.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "Both endpoints are inside the Battle Zone here, so p.18's correction has nothing to " +
                "do and this case shows the exchange itself rather than the correction after it.");
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "A Swap does not count as movement or placement (p.19), so no Movement action began.");
        }

        [Test]
        public void AnIllegalSwapTarget_IsRejectedAtomicallyAndThePendingChoiceSurvives()
        {
            var session = StartExchangeSkill(out _, out _, out _);
            ChooseBranch(session, SwapBranch);

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(BlueAt)))));

            Assert.That(illegal.Accepted, Is.False,
                "A space the selection never offered cannot be answered with, forged or stale.");
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal is atomic: the pending exchange is exactly what it was.");
        }

        [Test]
        public void APausedExchangeChoice_SurvivesSnapshotRestoreWithRederivedCandidates()
        {
            var session = StartExchangeSkill(out _, out _, out _);
            ChooseBranch(session, SwapBranch);
            var paused = session.Projection.PendingEffectChoice;

            var byteIdentical = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(byteIdentical.Round, byteIdentical.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A paused exchange round-trips byte-identically; nothing about it is a new wire field.");

            var payload = session.CaptureSnapshot();
            var restored = StartExchangeSkill(out _, out _, out var minionAt);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            Assert.That(
                restored.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer.");
            SelectValue(restored, minionAt);
            Assert.That(restored.Projection.PendingSkillResolution, Is.Null,
                "The restored exchange continues to the same completion the live one would.");
        }

        [Test]
        public void ASwap_ReplaysAndUndoesDeterministically()
        {
            var session = StartExchangeSkill(out var actorAt, out _, out var minionAt);
            var actorId = session.Projection.SelectedActor.ActorId;
            ChooseBranch(session, SwapBranch);

            var selection = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(minionAt));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(minionAt));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(actorAt),
                "Retained-prefix Undo puts both objects back, because a Swap is authoritative board " +
                "state the replay reaches.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void ASwapLeavingAMinionOutsideTheBattleZone_ReachesTheAfterActionCorrection()
        {
            // The acting hero stands outside the Battle Zone beside a minion inside it, so exchanging
            // the two puts an intrinsic minion miniature where p.18 says it may not stay. Milestone
            // 6.4a's correction runs at the action boundary and is reached by every Skill, so a Swap
            // inherits it rather than declaring one of its own.
            var session = StartExchangeSkill(
                true, out var actorAt, out var minion, out var minionAt);
            var board = RestoreBoard(session);
            Assert.That(
                BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, minionAt), Is.True);
            Assert.That(
                BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, actorAt), Is.False,
                "The fixture is only meaningful if the exchange really strands the minion.");

            ChooseBranch(session, SwapBranch);
            SelectValue(session, minionAt);

            var corrected = RestoreBoard(session);
            var correction = session.Projection.PendingDisplacedMinionCorrection;
            var landed = corrected.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;
            Assert.That(
                correction != null ||
                    BattleZoneQuery.IsInStartingBattleZone(corrected.MapDefinition, landed),
                Is.True,
                "p.18 corrects a displaced minion at the after-action checkpoint: either the owning " +
                "team is being asked where it returns, or a deterministic correction already ran.");
        }

        [Test]
        public void TheRemoveBranch_TakesTheChosenPieceOffTheBoardWithoutADefeat()
        {
            var session = StartExchangeSkill(out _, out var minion, out var minionAt);

            ChooseBranch(session, RemoveBranch);
            SelectValue(session, minionAt);

            var board = RestoreBoard(session);
            Assert.That(board.Minions.Any(value => value.MinionId.Equals(minion)), Is.False);
            Assert.That(board.Minions.Count, Is.EqualTo(11));
            Assert.That(
                board.FindUnitSupply().Select(unit => unit.MinionId), Has.Member(minion),
                "The declared destination is the supply, which p.19's Respawn ordering depends on.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "The Skill completes; no Defeat stage opened, and p.19 says outright not to collect " +
                "any coins - the operation publishes no reward fact for one to be read from.");
        }

        [Test]
        public void ARemove_SurvivesSnapshotRestoreAndUndoesDeterministically()
        {
            var session = StartExchangeSkill(out _, out var minion, out var minionAt);
            ChooseBranch(session, RemoveBranch);
            var selection = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(minionAt));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            var restored = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A committed Remove round-trips with no field the writer and reader disagree on.");
            Assert.That(
                restored.Board.FindUnitSupply().Select(unit => unit.MinionId), Has.Member(minion),
                "The supply is rebuilt from the roster minus the restored positions, so a Remove " +
                "widens it without anything about it reaching the wire.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(
                RestoreBoard(session).Minions.Any(value => value.MinionId.Equals(minion)), Is.True,
                "Retained-prefix Undo puts the miniature back on the board.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void ABranchBehindATargetSelection_IsOfferedEvenWhenItsLaterStepCannotApply()
        {
            // The recorded boundary of milestone 6.5ac's walk, not a defect this milestone introduces:
            // the walk short-circuits at the first thing that can apply, and a target selection can
            // always pause, so a branch whose selection has candidates is offered without its later
            // nodes being judged. Every subject since milestone 6.8k arrives that way - a role cannot
            // supply a coordinate - so direct Move, Place, Swap, Replace, and Remove all share it.
            // Here no Hazard token stands anywhere, so the Replace can apply nothing at all.
            var session = StartExchangeSkill(out _, out _, out _);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Has.Member(ReplaceBranch),
                "The branch is offered because its first step can be taken, which is the direction " +
                "6.5ac chose to err in: refuse what is provably empty, never disable what is merely " +
                "unreadable.");
            Assert.That(RestoreBoard(session).PlacedTokens, Is.Empty, "Nothing was placed to find out.");
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

        /// <summary>
        /// A session whose Red hero stands outside the Battle Zone beside a non-Heavy Red minion
        /// inside it, with the board-object Skill started and its mode choice pending.
        ///
        /// The minion is non-Heavy on purpose: p.13 makes a Heavy immune while friendly non-Heavy
        /// minions stand in the zone, which would make every case here an immunity case. The actor
        /// stands outside the zone on purpose too, so the Swap has an after-action correction to run.
        /// </summary>
        private static MatchSession StartExchangeSkill(
            out HexCoordinate actorAt, out MinionId minion, out HexCoordinate minionAt) =>
            StartExchangeSkill(false, out actorAt, out minion, out minionAt);

        private static MatchSession StartExchangeSkill(
            bool actorOutsideBattleZone,
            out HexCoordinate actorAt,
            out MinionId minion,
            out HexCoordinate minionAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var configured = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var setup = QuickGameSetupAuthority.Create(
                configured, configuration.Participants, map, configuration.TieBreakerState);

            // Which hero acts is decided by the committed cards' Initiative, not by where anybody
            // stands, so it is read from a throwaway session first and the real board is then built
            // around that hero. Guessing it would make these cases depend on a card matrix the
            // showcase conversions keep moving.
            var acting = ActingHero(map, setup, configuration);
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var candidate = setup.Minions
                .Where(value =>
                    value.MinionType != StandardMinionType.Heavy &&
                    BattleZoneQuery.IsInStartingBattleZone(map, value.Coordinate))
                .SelectMany(value => map.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                    .Where(space =>
                        HexDistanceQuery.Calculate(space.Coordinate, value.Coordinate) == 1 &&
                        !occupied.Contains(space.Coordinate) &&
                        BattleZoneQuery.IsInStartingBattleZone(map, space.Coordinate) !=
                            actorOutsideBattleZone)
                    .Select(space => new { Minion = value, Beside = space.Coordinate }))
                .First();

            minion = candidate.Minion.MinionId;
            minionAt = candidate.Minion.Coordinate;
            actorAt = candidate.Beside;

            var session = CreateSession(map, setup, configuration, acting, candidate.Beside);
            CommitShoveCards(session);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(acting),
                "The hero the board was built around must be the one whose turn this is.");
            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(value => value.ActionDefinitionId.Equals(ExchangeSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(ExchangeSkill),
                "The board-object Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        /// <summary>Which hero the committed shove cards give the first turn to.</summary>
        private static HeroId ActingHero(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            var probe = CreateSession(map, setup, configuration, default, default);
            CommitShoveCards(probe);
            return probe.Projection.SelectedActor.ActorId;
        }

        /// <summary>
        /// A session with the named hero standing on the named space and the other far away, or the
        /// setup's own hero positions when no space is named.
        /// </summary>
        private static MatchSession CreateSession(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            PlaceholderOneVsOneTestConfiguration configuration,
            HeroId placed,
            HexCoordinate at)
        {
            var occupancies = setup.Heroes
                .Select(hero => new HeroOccupancy(
                    hero.HeroId,
                    placed.IsDefined && hero.HeroId.Equals(placed) ? at : ElsewhereFor(hero.HeroId)))
                .ToArray();
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                occupancies,
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>A space well away from the Battle Zone, so the other hero is beside nothing.</summary>
        private static HexCoordinate ElsewhereFor(HeroId heroId) =>
            heroId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue
                ? new HexCoordinate(-2, -5)
                : BlueAt;

        private static void CommitShoveCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var heroId = session.Projection.Setup.Heroes
                    .Single(value => value.TeamSide == side).HeroId;
                var card = session.Projection.Cards
                    .Where(value =>
                        value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand)
                    .Single(value => value.Id.Value.Contains("shove"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }
    }
}
