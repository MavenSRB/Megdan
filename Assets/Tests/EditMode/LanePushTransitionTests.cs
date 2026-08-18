using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Transitions;
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
    /// Milestone 6.11. Rulebook p.8's Lane Push, completed.
    ///
    /// "1. Flip one Wave counter face-down ... 2. The next Zone towards the losing team's Throne
    /// becomes the new Battle Zone ... 3. Remove all minions in the previous Battle Zone. Respawn one
    /// minion of the matching type in each Spawn point in the new Battle Zone (If a Spawn point is
    /// occupied by a token, first remove that token, then respawn the minion)." And, of the mid-turn
    /// push: "If the Lane is pushed in the middle of a turn, the turn continues in the Initiative order
    /// as normal."
    ///
    /// This is the milestone's closing evidence, and it is a failing-then-passing observation rather
    /// than acceptance evidence in the 6.2a style, because there was real stale behaviour to observe: at
    /// the pre-fix revision both checkpoints refused with <c>UnimplementedLanePushConsumption</c> and the
    /// Battle Zone never moved.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class LanePushTransitionTests
    {
        private static readonly CardInstanceId RemoveCardId = new CardInstanceId("lane-push-transition-card");

        private static readonly ActionDefinitionId RemoveSkill =
            new ActionDefinitionId("lane-push-transition-skill");

        private static readonly HeroId RedHero =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly ZoneId StartingBattleZone = new ZoneId("starting-battle-zone");
        private static readonly ZoneId BlueApproach = new ZoneId("blue-approach");

        // -------------------------------------------------------------------------------------------
        // Acceptance checks 1 and 7: the whole transition runs, in p.8's order, before Initiative
        // continues
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnImmediateLanePush_CompletesTheWholeTransitionBeforeInitiativeContinues()
        {
            var session = StartRemoveSkillAgainstTheLastMinion();
            var board = RestoreBoard(session);

            Assert.That(session.Projection.PendingImmediateLanePush, Is.Null,
                "p.8 pushes the Lane immediately, so the request is consumed by the transition rather " +
                "than left standing at a checkpoint nothing reaches.");
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueApproach),
                "p.8 step 2: 'The next Zone towards the losing team's Throne becomes the new Battle " +
                "Zone.' Blue lost the push, so the Lane moves one verified step towards the Blue Throne.");
            Assert.That(
                board.Minions.Any(minion =>
                    board.MapDefinition.GetZoneForSpace(SpaceIdAt(board, minion.Coordinate))
                        .Id.Equals(StartingBattleZone)),
                Is.False,
                "p.8 step 3 opens with 'Remove all minions in the previous Battle Zone'.");
            Assert.That(
                board.Minions.Any(minion =>
                    board.MapDefinition.GetZoneForSpace(SpaceIdAt(board, minion.Coordinate))
                        .Id.Equals(BlueApproach)),
                Is.True,
                "and closes with 'Respawn one minion of the matching type in each Spawn point in the " +
                "new Battle Zone'.");
        }

        [Test]
        public void AFinishedImmediatePush_LetsTheTurnContinueInInitiativeOrder()
        {
            var session = StartRemoveSkillAgainstTheLastMinion();
            var blueCard = session.Projection.Cards.First(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.TurnArea);

            var held = session.Execute(DirectorCommand.PerformHold(blueCard.Id));

            Assert.That(held.Accepted, Is.True,
                "p.8: 'If the Lane is pushed in the middle of a turn, the turn continues in the " +
                "Initiative order as normal.' The push is finished, so the next actor may act. " +
                $"Detail: {held.Detail}");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 1, on the transition's own seam: the stages run in p.8's order
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ThePush_RunsTheStagesInThePagesOrder()
        {
            var fixture = CreateBoardFixture();

            var result = fixture.Push(TeamSide.Red, TeamSide.Blue);

            Assert.That(result.Finished.Outcomes.Select(outcome => outcome.Stage), Is.EqualTo(new[]
            {
                LanePushTransitionStage.FlipWaveCounter,
                LanePushTransitionStage.AdvanceBattleZone,
                LanePushTransitionStage.RemoveAndRespawnMinions
            }), "p.8 numbers its three steps, and the transition reports them in that order.");
            Assert.That(fixture.Transitions.PendingStep, Is.Null,
                "Nothing blocks a Spawn point here, so the push asked nobody anything.");
            Assert.That(
                fixture.Board.FindMinionsInZone(StartingBattleZone), Is.Empty,
                "'Remove all minions in the previous Battle Zone.'");
            Assert.That(
                fixture.Board.FindMinionsInZone(BlueApproach).Count, Is.EqualTo(11),
                "'Respawn one minion of the matching type in each Spawn point in the new Battle Zone.' " +
                "blue-approach declares six Blue and five Red minion Spawn points, and both teams have " +
                "six miniatures - so eleven stand up and, exactly as p.8's own diagram says, 'One Red " +
                "minion will not be respawned.'");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 2: a token-covered Spawn point removes its token before the respawn
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ATokenCoveredSpawnPoint_LosesItsTokenAndThenRespawns()
        {
            var fixture = CreateBoardFixture();
            var covered = fixture.MinionSpawnPointsIn(BlueApproach).First();
            fixture.PlaceToken(covered.Coordinate);

            var result = fixture.Push(TeamSide.Red, TeamSide.Blue);

            Assert.That(result.IsPaused, Is.False,
                "p.8 clears a token itself - 'first remove that token, then respawn the minion' - so " +
                "nobody is asked anything about it.");
            Assert.That(fixture.Board.HasTokenAt(covered.Coordinate), Is.False,
                "The token was removed rather than pushed aside or left under the miniature.");
            Assert.That(fixture.Board.IsEmptyPlayableSpace(covered.Coordinate), Is.False,
                "And the Spawn point holds its minion, so the clearance happened before the respawn " +
                "rather than instead of it.");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance checks 3 and 4: a non-token obstacle asks the owning team, Tie-Breaker side first
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ANonTokenObstacle_AsksTheOwningTeamForANearestEmptyBattleZoneSpace()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.Red);
            var blocked = fixture.BlockedSpawnPoints.Single();

            var result = fixture.Push(TeamSide.Red, TeamSide.Blue);

            Assert.That(result.IsPaused, Is.True,
                "p.8's sidebar: 'the owning team must place that minion in the nearest empty space of " +
                "their choice in the Battle Zone'. A choice is a pause.");
            var pending = result.PendingStep;
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red),
                "The owning team chooses, which is the team whose Spawn point it is rather than the " +
                "team that won the push.");
            Assert.That(pending.StepId, Is.EqualTo(LanePushSystemTransition.RespawnStepId));
            Assert.That(pending.ItemId.Value, Does.StartWith(blocked.SpawnPoint.Id.Value),
                "The continuation records which Spawn point this pause is about.");
            Assert.That(pending.ItemId.Value, Does.Contain("red-"),
                "and which miniature it drew for it, which a re-derivation could not reproduce once " +
                "that miniature had left the supply.");

            var nearest = NearestEmptyIn(fixture, blocked.Coordinate, BlueApproach);
            Assert.That(
                pending.Options.Select(option => option.Value).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(nearest.OrderBy(value => value, StringComparer.Ordinal)),
                "Every nearest empty space of the Battle Zone is offered and nothing else is: not a " +
                "space further away, and not one outside the Zone.");
        }

        [Test]
        public void AnAnsweredPlacement_PutsTheMinionOnTheChosenSpaceAndFinishesThePush()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.Red);
            var paused = fixture.Push(TeamSide.Red, TeamSide.Blue);
            var chosen = paused.PendingStep.Invocations.First();
            var destination = fixture.CoordinateOf(chosen.OptionId.Value);

            var answered = fixture.Answer(chosen);

            Assert.That(answered.IsPaused, Is.False, "One blocked Spawn point owes one Place.");
            Assert.That(fixture.Board.IsEmptyPlayableSpace(destination), Is.False,
                "The chosen space now holds the minion the blocked Spawn point owed.");
            Assert.That(
                answered.Finished.Outcomes.Last(),
                Is.EqualTo(LanePushStageOutcome.Completed(
                    LanePushTransitionStage.RemoveAndRespawnMinions)),
                "and p.8's third step is reported complete only once the last Place has been made.");
        }

        [Test]
        public void WhenBothTeamsOweAPlace_TheTieBreakerSideIsAskedFirstAndTheCoinIsNotFlipped()
        {
            foreach (var coin in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.None);
                Assert.That(fixture.BlockedSpawnPoints.Select(value => value.Side).Distinct().Count(),
                    Is.EqualTo(2),
                    "The case only says anything if both teams owe a Place.");

                var paused = fixture.Push(TeamSide.Red, TeamSide.Blue, new TieBreakerState(coin));

                Assert.That(paused.PendingStep.ChoosingSide, Is.EqualTo(coin),
                    "p.8's sidebar: 'starting with the team whose symbol is on the tie breaker coin'. " +
                    $"The coin shows {coin}, so {coin} places first.");
                Assert.That(fixture.LanePush.Running.TieBreakerSide, Is.EqualTo(coin),
                    "and the push carries the face the coin was showing when it began rather than " +
                    "flipping it: p.8 flips the coin for Initiative ties, not for a Lane Push.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 5: the continuation is the exact resume location, across every path
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ARefusedAnswer_LeavesTheContinuationExactlyAsItWas()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.None);
            var paused = fixture.Push(TeamSide.Red, TeamSide.Blue, new TieBreakerState(TeamSide.Red));
            var pending = paused.PendingStep;
            var minionsBefore = fixture.Board.FindMinionsInZone(BlueApproach).Count;

            var wrongChooser = fixture.TryAnswer(new SystemTransitionChoiceCommand(
                pending.InstanceId, pending.StepId, pending.ItemId,
                pending.ChoosingSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red,
                pending.Options[0]));
            var wrongItem = fixture.TryAnswer(new SystemTransitionChoiceCommand(
                pending.InstanceId, pending.StepId, new SystemTransitionItemId("not-this-minion"),
                pending.ChoosingSide, pending.Options[0]));
            var illegalOption = fixture.TryAnswer(new SystemTransitionChoiceCommand(
                pending.InstanceId, pending.StepId, pending.ItemId, pending.ChoosingSide,
                new SystemTransitionOptionId("q99_r99")));
            var wrongInstance = fixture.TryAnswer(new SystemTransitionChoiceCommand(
                new SystemTransitionInstanceId("some-other-push"), pending.StepId, pending.ItemId,
                pending.ChoosingSide, pending.Options[0]));

            Assert.That(wrongChooser.RejectionReason,
                Is.EqualTo(SystemTransitionRejectionReason.WrongChooser));
            Assert.That(wrongItem.RejectionReason,
                Is.EqualTo(SystemTransitionRejectionReason.StaleItem),
                "A step that stops once per minion has a right answer for each one, so answering the " +
                "wrong minion is exactly as wrong as answering the wrong step.");
            Assert.That(illegalOption.RejectionReason,
                Is.EqualTo(SystemTransitionRejectionReason.IllegalOption));
            Assert.That(wrongInstance.RejectionReason,
                Is.EqualTo(SystemTransitionRejectionReason.WrongTransitionInstance));

            Assert.That(fixture.Transitions.PendingStep.ItemId, Is.EqualTo(pending.ItemId));
            Assert.That(fixture.Transitions.PendingStep.Options, Is.EqualTo(pending.Options));
            Assert.That(fixture.Board.FindMinionsInZone(BlueApproach).Count, Is.EqualTo(minionsBefore),
                "Every refusal leaves the board exactly as it was, so a refused answer is " +
                "indistinguishable from one that never arrived.");
        }

        [Test]
        public void AnAnsweredPlacement_CannotBeAnsweredAgain()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.None);
            var paused = fixture.Push(TeamSide.Red, TeamSide.Blue, new TieBreakerState(TeamSide.Red));
            var first = paused.PendingStep;

            Assert.That(fixture.Answer(first.Invocations[0]).IsPaused, Is.True,
                "The second team still owes its own Place.");

            var repeated = fixture.TryAnswer(first.Invocations[0]);

            Assert.That(repeated.RejectionReason,
                Is.EqualTo(SystemTransitionRejectionReason.StaleItem),
                "A completed respawn cannot be repeated - which is what keeps a resend, a double " +
                "click, or a replayed stale command from placing the same miniature twice.");
            Assert.That(fixture.Transitions.PendingStep.ItemId, Is.Not.EqualTo(first.ItemId));
            Assert.That(
                fixture.Transitions.PendingStep.CompletedItemIds.Count,
                Is.EqualTo(first.CompletedItemIds.Count + 1),
                "and the continuation's record of what has happened grew by exactly that one item.");
            Assert.That(
                fixture.Transitions.PendingStep.CompletedItemIds.Any(item => item.Equals(first.ItemId)),
                Is.True,
                "Every Spawn point the respawn has dealt with is recorded, the uncontested ones " +
                "included - otherwise 'which Spawn points are still owed' would have to be re-derived " +
                "from a board the respawn has been changing.");
        }

        [Test]
        public void APausedPush_ReconstructsEquivalentlyAfterRestore()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.None);
            var paused = fixture.Push(TeamSide.Red, TeamSide.Blue, new TieBreakerState(TeamSide.Blue));
            Assert.That(fixture.Answer(paused.PendingStep.Invocations[0]).IsPaused, Is.True);
            var pending = fixture.Transitions.PendingStep;

            var restored = fixture.RestoreTransitions();

            Assert.That(restored.PendingStep, Is.Not.Null);
            Assert.That(restored.PendingStep.ItemId, Is.EqualTo(pending.ItemId));
            Assert.That(restored.PendingStep.ChoosingSide, Is.EqualTo(pending.ChoosingSide));
            Assert.That(restored.PendingStep.Options, Is.EqualTo(pending.Options));
            Assert.That(restored.PendingStep.CompletedStepIds, Is.EqualTo(pending.CompletedStepIds),
                "The Wave flip, the Zone advance and the removal are all recorded as done, so no " +
                "resumption can run any of them a second time.");
            Assert.That(restored.PendingStep.CompletedItemIds, Is.EqualTo(pending.CompletedItemIds),
                "and so is the Place that has already been made.");
        }

        [Test]
        public void APausedPush_IsRefusedWhenTheRestoredStateDoesNotReproduceIt()
        {
            var fixture = CreateBoardFixture(blockedSpawnPointOwners: TeamSide.None);
            var paused = fixture.Push(TeamSide.Red, TeamSide.Blue, new TieBreakerState(TeamSide.Red));
            var pending = paused.PendingStep;

            // A payload claiming a chooser the board does not produce for that item.
            var tampered = new PendingSystemTransitionStep(
                pending.InstanceId,
                pending.DefinitionId,
                pending.StepId,
                pending.ItemId,
                pending.ChoosingSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red,
                pending.Options,
                pending.CompletedStepIds,
                pending.CompletedItemIds);

            Assert.That(
                () => SystemTransitionAuthority.Restore(
                    new SystemTransitionRestoreState(tampered),
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(fixture.LanePush)),
                Throws.ArgumentException,
                "A derived step has no catalog options to be checked against, so it is re-derived over " +
                "the restored board instead - and a payload the board does not reproduce is refused " +
                "rather than repaired.");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 5, end to end: a paused push crosses restore, replay and Undo
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APausedMidTurnPush_StopsTheTurnAndSurvivesASnapshotByteForByte()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(blockBlueApproachSpawnPoints: true);
            var pending = session.Projection.PendingSystemTransitionStep;
            var payload = session.CaptureSnapshot();

            Assert.That(pending, Is.Not.Null,
                "An obstacle stands on a Spawn point of the new Battle Zone, so p.8's sidebar owes its " +
                "owning team a Place and the push stops on it.");
            Assert.That(pending.StepId, Is.EqualTo(LanePushSystemTransition.RespawnStepId));
            Assert.That(session.Projection.PendingImmediateLanePush, Is.Not.Null,
                "The request is still held, because the push it began has not finished - which is the " +
                "same fact as 'an Initiative advance is still owed'.");

            var blueCard = session.Projection.Cards.First(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.TurnArea);
            var refused = session.Execute(DirectorCommand.PerformHold(blueCard.Id));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.Detail, Does.Contain(
                TurnResolutionRejectionReason.IncompleteLanePushTransition.ToString()),
                "p.8's turn continues 'as normal' only once the push is done, so the next actor waits.");

            var restored = CreateSessionLike(session);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(
                restored.Projection.PendingSystemTransitionStep.ItemId, Is.EqualTo(pending.ItemId));
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "The restored match writes the same bytes, so the paused push round-trips rather than " +
                "being approximated.");
        }

        [Test]
        public void AnAnsweredPlaceReplaysAndUndoesExactly()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(blockBlueApproachSpawnPoints: true);
            var pending = session.Projection.PendingSystemTransitionStep;

            var answered = session.Execute(
                DirectorCommand.AnswerSystemTransitionStep(pending.Invocations[0]));
            Assert.That(answered.Accepted, Is.True, answered.Detail);
            var expected = session.CaptureSnapshot();

            // Undo removes the last command and replays the retained prefix, so the state that comes
            // back is rebuilt from the recorded commands rather than copied.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(
                session.Projection.PendingSystemTransitionStep.ItemId, Is.EqualTo(pending.ItemId),
                "The undone Place is genuinely undone: the push is back on the minion it was waiting " +
                "for, and the Wave counter it already spent was re-spent by the replayed prefix rather " +
                "than spent twice.");
            Assert.That(session.Projection.PendingImmediateLanePush, Is.Not.Null);

            Assert.That(session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0])).Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(expected),
                "Replaying the same answer reproduces the same authoritative state byte for byte.");
        }

        // -------------------------------------------------------------------------------------------
        // The fixture: a real turn on the official map, driven to the mid-turn checkpoint
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A played turn in which the Red hero's Skill removes the one Blue minion left in the Battle
        /// Zone, which is p.8's second mid-turn sentence and the cause milestone 6.9 already emits.
        ///
        /// The card is declared by this fixture rather than added to the configured hand, for the reason
        /// milestone 6.9's fixture recorded: no shipped card removes a minion, and adding one would move
        /// every pinned digest for a rule that needs no content.
        /// </summary>
        private static MatchSession StartRemoveSkillAgainstTheLastMinion(
            bool blockBlueApproachSpawnPoints = false)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue &&
                minion.MinionType != StandardMinionType.Heavy &&
                BattleZoneQuery.IsInStartingBattleZone(map, minion.Coordinate));

            var minions = setup.Minions
                .Where(minion =>
                    minion.TeamSide != TeamSide.Blue || minion.MinionId.Equals(survivor.MinionId))
                .ToArray();
            var relocatedSetup = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, setup.Heroes,
                minions);

            var definition = CreateRemoveSkillDefinition(survivor.Coordinate);
            var instance = CardInstance.CreateInHand(
                RemoveCardId,
                definition.Reference,
                configuration.RedHand.Instances[0].OwnerId);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions.Concat(new[] { definition }),
                configuration.CardInstances.Concat(new[] { instance }),
                configuration.TieBreakerState,
                map.Reference,
                relocatedSetup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                blockBlueApproachSpawnPoints
                    ? BlueApproachSpawnPointObstacles(map)
                    : Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(
                new MatchSessionInitialState(scenario, relocatedSetup, new[] { map }));

            CommitBothSides(session);
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(RemoveSkill));
            var started = session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId));
            Assert.That(started.Accepted, Is.True, started.Detail);
            return session;
        }

        /// <summary>
        /// An obstacle on the first Red-owned and first Blue-owned minion Spawn point of the Zone this
        /// push will reach, so p.8's sidebar has something to ask about.
        /// </summary>
        private static IReadOnlyList<ObstacleOccupancy> BlueApproachSpawnPointObstacles(
            BoardMapDefinition map)
        {
            var candidates = map.SpawnPoints
                .Where(spawn => spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal))
                .Select(spawn => new
                {
                    Spawn = spawn,
                    Space = map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId))
                })
                .Where(value => value.Space.ZoneId.HasValue &&
                    value.Space.ZoneId.Value.Equals(BlueApproach))
                .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                .ToArray();
            return new[] { "red", "blue" }
                .Select(teamId => candidates.First(value =>
                    string.Equals(value.Spawn.TeamId, teamId, StringComparison.Ordinal)))
                .Select(value => new ObstacleOccupancy(
                    new ObstacleId($"lane-push-block-{value.Spawn.TeamId}"), value.Space.Coordinate))
                .ToArray();
        }

        /// <summary>A second session over the same scenario, for a restore to be measured against.</summary>
        private static MatchSession CreateSessionLike(MatchSession session) =>
            StartRemoveSkillAgainstTheLastMinion(blockBlueApproachSpawnPoints: true);

        private static void CommitBothSides(MatchSession session)
        {
            var owner = session.Projection.Cards.Single(card => card.Id.Equals(RemoveCardId)).OwnerId;
            var red = session.Execute(DirectorCommand.CommitCard(owner, RemoveCardId));
            Assert.That(red.Accepted, Is.True, red.Detail);

            var other = session.Projection.Cards.First(card =>
                !card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            var blue = session.Execute(DirectorCommand.CommitCard(other.OwnerId, other.Id));
            Assert.That(blue.Accepted, Is.True, blue.Detail);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(RemoveCardId));
        }

        private static CardDefinition CreateRemoveSkillDefinition(HexCoordinate subject)
        {
            var root = new EffectNodeId("lane-push-transition-root");
            var effect = new EffectDefinition(
                new EffectDefinitionId("lane-push-transition-effect"),
                "phase-6-11-test-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("subject", EffectValue.Space(subject)),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer((int)RemoveEligiblePieceKind.GamePiece))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId("lane-push-transition-test"),
                "phase-6-11-test-rev-1",
                "TEST - Lane Push Transition Remove",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        RemoveSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        // -------------------------------------------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The authoritative board behind a session, read the way every fixture here reads it: through
        /// the snapshot the session publishes, because the board itself is not a projection surface.
        /// </summary>
        private static GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority RestoreBoard(
            MatchSession session)
        {
            return AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions).Board;
        }

        private static SpaceId SpaceIdAt(
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            return board.MapDefinition.Spaces
                .Single(space =>
                    space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(coordinate))
                .Id;
        }

        private static IReadOnlyList<string> NearestEmptyIn(
            BoardFixture fixture, HexCoordinate origin, ZoneId zoneId)
        {
            var empty = fixture.Board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable &&
                    space.ZoneId.HasValue && space.ZoneId.Value.Equals(zoneId) &&
                    fixture.Board.IsEmptyPlayableSpace(space.Coordinate))
                .Select(space => space.Coordinate);
            return GuardsDigital.Core.Rules.Movement.NearestQuery.FindNearest(origin, empty)
                .Select(coordinate => SpaceIdAt(fixture.Board, coordinate).Value)
                .ToArray();
        }

        // -------------------------------------------------------------------------------------------
        // The board fixture: the official map, driven through the transition's own seam
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A board the push can be driven over directly, without a session around it.
        ///
        /// The obstacle cases need Spawn points of the *new* Battle Zone standing under something, and
        /// a session's initial scenario cannot place one there without also changing every pinned
        /// digest; the board seam is where the rule lives and is what these cases are about. The
        /// end-to-end path is covered by the two mid-turn cases above.
        /// </summary>
        private sealed class BoardFixture
        {
            public BoardFixture(
                GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
                IReadOnlyList<BlockedSpawnPoint> blockedSpawnPoints)
            {
                Board = board;
                BlockedSpawnPoints = blockedSpawnPoints;
                LanePush = new LanePushTransitionAuthority(board, 3);
                Transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(LanePush));
                Consumption = new LanePushConsumption(LanePush, Transitions);
            }

            public GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority Board { get; }
            public IReadOnlyList<BlockedSpawnPoint> BlockedSpawnPoints { get; }
            public LanePushTransitionAuthority LanePush { get; }
            public SystemTransitionAuthority Transitions { get; }
            public LanePushConsumption Consumption { get; }

            public LanePushConsumptionResult Push(TeamSide winning, TeamSide losing) =>
                Push(winning, losing, new TieBreakerState(TeamSide.Red));

            public LanePushConsumptionResult Push(
                TeamSide winning, TeamSide losing, TieBreakerState tieBreakerState)
            {
                var result = Consumption.TryConsume(
                    new LanePushRequestId("lane-push-transition-fixture"), winning, losing,
                    tieBreakerState);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                return result;
            }

            public LanePushConsumptionResult Answer(SystemTransitionChoiceCommand command)
            {
                var answered = Transitions.TryAnswer(command);
                Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
                return Consumption.ReportAfterAnswer();
            }

            public SystemTransitionResult TryAnswer(SystemTransitionChoiceCommand command) =>
                Transitions.TryAnswer(command);

            /// <summary>
            /// The continuation rebuilt from what a payload would carry, over this same board - which is
            /// what a restore does, minus the bytes.
            /// </summary>
            public SystemTransitionAuthority RestoreTransitions() =>
                SystemTransitionAuthority.Restore(
                    Transitions.CaptureRestoreState(),
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(LanePush));

            public IReadOnlyList<BlockedSpawnPoint> MinionSpawnPointsIn(ZoneId zoneId) =>
                Board.MapDefinition.SpawnPoints
                    .Where(spawn => spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal))
                    .Select(spawn => new
                    {
                        Spawn = spawn,
                        Space = Board.MapDefinition.Spaces.Single(space => space.Id.Equals(spawn.SpaceId))
                    })
                    .Where(value => value.Space.ZoneId.HasValue && value.Space.ZoneId.Value.Equals(zoneId))
                    .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                    .Select(value => new BlockedSpawnPoint(
                        value.Spawn,
                        string.Equals(value.Spawn.TeamId, "red", StringComparison.Ordinal)
                            ? TeamSide.Red
                            : TeamSide.Blue,
                        value.Space.Coordinate))
                    .ToArray();

            public void PlaceToken(HexCoordinate coordinate) =>
                Board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("lane-push-fixture-token"),
                    SyntheticComponentDefinitions.CreateCatalog().LoadToken(
                        SyntheticComponentDefinitions.HazardV4),
                    RedHero,
                    RedHero,
                    new CardInstanceId("lane-push-fixture-card"),
                    coordinate));

            public HexCoordinate CoordinateOf(string spaceIdValue) =>
                Board.MapDefinition.Spaces.Single(space => space.Id.Value == spaceIdValue).Coordinate;
        }

        private sealed class BlockedSpawnPoint
        {
            public BlockedSpawnPoint(BoardMapSpawnPoint spawnPoint, TeamSide side, HexCoordinate coordinate)
            {
                SpawnPoint = spawnPoint;
                Side = side;
                Coordinate = coordinate;
            }

            public BoardMapSpawnPoint SpawnPoint { get; }
            public TeamSide Side { get; }
            public HexCoordinate Coordinate { get; }
        }

        /// <summary>
        /// The official map with the configured setup, and optionally an obstacle standing on the first
        /// Spawn point of the new Battle Zone belonging to each named side.
        /// </summary>
        private static BoardFixture CreateBoardFixture(TeamSide? blockedSpawnPointOwners = null)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                PlaceholderOneVsOneParticipants.Create(),
                map,
                new TieBreakerState(TeamSide.Red));

            var blocked = new List<BlockedSpawnPoint>();
            var obstacles = new List<ObstacleOccupancy>();
            if (blockedSpawnPointOwners.HasValue)
            {
                var sides = blockedSpawnPointOwners.Value == TeamSide.None
                    ? new[] { TeamSide.Red, TeamSide.Blue }
                    : new[] { blockedSpawnPointOwners.Value };
                var candidates = map.SpawnPoints
                    .Where(spawn => spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal))
                    .Select(spawn => new
                    {
                        Spawn = spawn,
                        Space = map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId))
                    })
                    .Where(value => value.Space.ZoneId.HasValue &&
                        value.Space.ZoneId.Value.Equals(BlueApproach))
                    .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                    .ToArray();
                foreach (var side in sides)
                {
                    var teamId = side == TeamSide.Red ? "red" : "blue";
                    var chosen = candidates.First(value =>
                        string.Equals(value.Spawn.TeamId, teamId, StringComparison.Ordinal));
                    blocked.Add(new BlockedSpawnPoint(chosen.Spawn, side, chosen.Space.Coordinate));
                    obstacles.Add(new ObstacleOccupancy(
                        new ObstacleId($"lane-push-block-{teamId}"), chosen.Space.Coordinate));
                }
            }

            var board = new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                obstacles,
                setup.Minions);
            return new BoardFixture(board, blocked);
        }
    }
}
